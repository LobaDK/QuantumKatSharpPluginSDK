using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuantumKat.PluginSDK.Core.Interfaces;

namespace QuantumKat.PluginSDK.Core;

/// <summary>
/// Manages the loading, initialization, and lifecycle of plugins.
/// </summary>
public class PluginManager
{
    private readonly List<(IPlugin plugin, PluginLoadContext context)> _loadedPlugins = [];
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;
    private readonly IPluginServiceProvider _sharedServiceProvider;
    private readonly PluginEventRegistry _eventRegistry = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginManager"/> class.
    /// </summary>
    /// <param name="sharedServiceProvider">The shared service provider for managing services across the application and plugins.</param>
    /// <param name="configuration">The configuration settings.</param>
    /// <param name="logger">The logger instance.</param>
    public PluginManager(IPluginServiceProvider sharedServiceProvider, IConfiguration configuration, ILogger logger)
    {
        _sharedServiceProvider = sharedServiceProvider ?? throw new ArgumentNullException(nameof(sharedServiceProvider));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private class PluginMetadata
    {
        public required Type PluginType { get; set; }
        public required string Name { get; set; }
        public required string Version { get; set; }
        public required Dictionary<string, List<string>> Dependencies { get; set; }
        public required PluginLoadContext LoadContext { get; set; }
        public required string Path { get; set; }
    }

    /// <summary>
    /// Loads and initializes plugins from the specified collection of plugin assembly file paths.
    /// </summary>
    /// <param name="pluginPaths">
    /// An <see cref="IEnumerable{String}"/> containing the file paths to the plugin assemblies to load.
    /// </param>
    /// <remarks>
    /// For each plugin assembly path, this method:
    /// <list type="number">
    /// <item>Creates a new <c>PluginLoadContext</c> for isolation.</item>
    /// <item>Loads the assembly from the specified path.</item>
    /// <item>Searches for a non-abstract type implementing <see cref="IPlugin"/>.</item>
    /// <item>Instantiates the plugin and initializes it with a <see cref="PluginBootstrapContext"/>.</item>
    /// <item>Adds the loaded plugin and its context to the internal collection.</item>
    /// </list>
    /// If any step fails, the error is logged and the method continues with the next plugin.
    /// </remarks>
    public void LoadPlugins(IEnumerable<string> pluginPaths)
    {
        var discovered = new List<PluginMetadata>();

        foreach (var pluginDll in pluginPaths)
        {
            if (string.IsNullOrWhiteSpace(pluginDll) || !File.Exists(pluginDll))
            {
                _logger.LogWarning("Plugin path is null, empty, or does not exist: {pluginPath}", pluginDll);
                continue;
            }

            try
            {
                var context = new PluginLoadContext(pluginDll);
                var assembly = context.LoadFromAssemblyPath(pluginDll);
                var pluginType = assembly.GetTypes()
                    .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract);

                if (pluginType == null) continue;

                if (Activator.CreateInstance(pluginType) is not IPlugin pluginInstance)
                {
                    _logger.LogError("Failed to create an instance of plugin type: {plugintype}", pluginType.FullName);
                    continue;
                }

                discovered.Add(new PluginMetadata
                {
                    PluginType = pluginType,
                    Name = pluginInstance.Name,
                    Version = pluginInstance.Version,
                    Dependencies = pluginInstance.PluginDependencies ?? [],
                    LoadContext = context,
                    Path = pluginDll
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin metadata: {pluginPath}", pluginDll);
            }
        }

        var nameMap = discovered.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        var sorted = new List<PluginMetadata>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        bool Visit(PluginMetadata plugin, Stack<string> stack)
        {
            if (visited.Contains(plugin.Name))
                return true;
            if (stack.Contains(plugin.Name))
            {
                _logger.LogError("Circular dependency detected: {cycle}", string.Join(" -> ", stack.Append(plugin.Name)));
                return false;
            }
            stack.Push(plugin.Name);

            foreach (var dep in plugin.Dependencies)
            {
                if (!nameMap.TryGetValue(dep.Key, out var depPlugin))
                {
                    _logger.LogError("Missing dependency: {plugin} requires {dependency}", plugin.Name, dep.Key);
                    return false;
                }
                foreach (var versionReq in dep.Value)
                {
                    if (!CheckVersion(depPlugin.Version, versionReq))
                    {
                        _logger.LogError("Version mismatch: {plugin} requires {dep} {ver}, but found {actual}",
                            plugin.Name, dep.Key, versionReq, depPlugin.Version);
                        return false;
                    }
                }
                if (!Visit(depPlugin, stack))
                    return false;
            }
            stack.Pop();
            visited.Add(plugin.Name);
            if (!sorted.Contains(plugin))
                sorted.Add(plugin);
            return true;
        }

        foreach (var plugin in discovered)
        {
            if (!Visit(plugin, new Stack<string>()))
            {
                _logger.LogError("Failed to resolve dependencies for plugin: {plugin}", plugin.Name);
                continue;
            }
        }

        foreach (var meta in sorted)
        {
            try
            {
                if (Activator.CreateInstance(meta.PluginType) is not IPlugin pluginInstance)
                {
                    _logger.LogError("Failed to create an instance of plugin type: {plugintype}", meta.PluginType.FullName);
                    continue;
                }
                var bootstrapContext = new PluginBootstrapContext
                {
                    Configuration = _configuration,
                    Logger = _logger,
                    CoreServices = _sharedServiceProvider.ServiceProvider,
                    PluginDirectory = Path.GetDirectoryName(meta.Path) ?? string.Empty,
                    EventRegistry = _eventRegistry,
                    SharedServiceProvider = _sharedServiceProvider
                };
                pluginInstance.Initialize(bootstrapContext);
                _loadedPlugins.Add((pluginInstance, meta.LoadContext));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize plugin: {plugin}", meta.Name);
            }
        }
    }

    /// <summary>
    /// Checks if the given <paramref name="actual"/> version string satisfies the specified <paramref name="requirement"/>.
    /// </summary>
    /// <param name="actual">The actual version string to check.</param>
    /// <param name="requirement">
    /// The version requirement string. Supported formats:
    /// <list type="bullet">
    /// <item><description><c>==x.y.z</c>: Equal to version <c>x.y.z</c></description></item>
    /// <item><description><c>&gt;x.y.z</c>: Greater than version <c>x.y.z</c></description></item>
    /// <item><description><c>&lt;x.y.z</c>: Less than version <c>x.y.z</c></description></item>
    /// <item><description><c>&gt;=x.y.z</c>: Greater than or equal to version <c>x.y.z</c></description></item>
    /// <item><description><c>&lt;=x.y.z</c>: Less than or equal to version <c>x.y.z</c></description></item>
    /// <item><description>Empty or whitespace: Always returns <c>true</c></description></item>
    /// </list>
    /// </param>
    /// <returns>
    /// <c>true</c> if <paramref name="actual"/> satisfies the <paramref name="requirement"/>; otherwise, <c>false</c>.
    /// </returns>
    private static bool CheckVersion(string actual, string requirement)
    {
        if (string.IsNullOrWhiteSpace(requirement)) return true;
        if (requirement.StartsWith("=="))
            return actual == requirement[2..];
        if (requirement.StartsWith(">="))
            return string.Compare(actual, requirement[2..], StringComparison.Ordinal) >= 0;
        if (requirement.StartsWith("<="))
            return string.Compare(actual, requirement[2..], StringComparison.Ordinal) <= 0;
        if (requirement.StartsWith(">"))
            return string.Compare(actual, requirement[1..], StringComparison.Ordinal) > 0;
        if (requirement.StartsWith("<"))
            return string.Compare(actual, requirement[1..], StringComparison.Ordinal) < 0;
        return actual == requirement;
    }

    /// <summary>
    /// Registers all services provided by the loaded plugins into the shared service provider.
    /// Iterates through each loaded plugin and invokes its <c>RegisterServices</c> method,
    /// allowing the plugin to add its services to the application's dependency injection container.
    /// After all services are registered, the service provider is rebuilt to ensure consistency.
    /// </summary>
    public void RegisterAllPluginServices()
    {
        _sharedServiceProvider.RegisterServicesAndRebuild(services =>
        {
            foreach (var (plugin, _) in _loadedPlugins)
            {
                plugin.RegisterServices(services);
            }
        });
    }

    /// <summary>
    /// Asynchronously starts all loaded plugins that implement <see cref="IThreadedPlugin"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation of starting all threaded plugins.</returns>
    public async Task StartAllPluginsAsync(CancellationToken cancellationToken)
    {
        List<IThreadedPlugin> threadedPlugins = [.. _loadedPlugins
            .Select(lp => lp.plugin)
            .OfType<IThreadedPlugin>()];

        if (threadedPlugins.Count == 0)
        {
            _logger.LogInformation("No threaded plugins to start...");
            return;
        }

        var tasks = new List<Task>();
        foreach (IThreadedPlugin threadedPlugin in threadedPlugins)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await threadedPlugin.StartAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Plugin '{plugin.Name}' failed to start.", threadedPlugin.Name);
                }
            }, cancellationToken));
        }
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Asynchronously stops all loaded plugins that implement <see cref="IThreadedPlugin"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation of stopping all threaded plugins.</returns>
    public async Task StopAllPluginsAsync(CancellationToken cancellationToken)
    {
        List<IThreadedPlugin> threadedPlugins = [.. _loadedPlugins
            .Select(lp => lp.plugin)
            .OfType<IThreadedPlugin>()];

        if (threadedPlugins.Count == 0)
        {
            _logger.LogInformation("No threaded plugins to stop...");
            return;
        }

        var tasks = new List<Task>();
        foreach (IThreadedPlugin threadedPlugin in threadedPlugins)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await threadedPlugin.StopAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Plugin '{plugin.Name}' failed to stop.", threadedPlugin.Name);
                }
            }, cancellationToken));
        }
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Asynchronously starts a specific plugin that implements <see cref="IThreadedPlugin"/>.
    /// </summary>
    /// <param name="plugin">The plugin to start.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task StartPluginAsync(IThreadedPlugin plugin, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        try
        {
            await plugin.StartAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plugin '{plugin.Name}' failed to start.", plugin.Name);
        }
    }

    /// <summary>
    /// Asynchronously stops a specific plugin that implements <see cref="IThreadedPlugin"/>.
    /// </summary>
    /// <param name="plugin">The plugin to stop.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task StopPluginAsync(IThreadedPlugin plugin, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        try
        {
            await plugin.StopAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plugin '{plugin.Name}' failed to stop.", plugin.Name);
        }
    }

    /// <summary>
    /// Dispatches a message to all registered plugin handlers.
    /// </summary>
    /// <param name="message">The <see cref="SocketMessage"/> to dispatch.</param>
    public async Task DispatchMessageAsync(SocketMessage message)
    {
        await _eventRegistry.DispatchMessageAsync(message);
    }
}

