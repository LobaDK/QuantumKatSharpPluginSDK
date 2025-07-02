using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuantumKat.PluginSDK.Core.Interfaces;

namespace QuantumKat.PluginSDK.Core;

/// <summary>
/// Manages the loading, initialization, and lifecycle of plugins.
/// </summary>
public class PluginManager(IServiceCollection services, IConfiguration configuration, ILogger logger, IServiceProvider coreServices)
{
    private readonly List<(IPlugin plugin, PluginLoadContext context)> _loadedPlugins = [];
    private readonly IServiceCollection _serviceCollection = services;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger _logger = logger;
    private readonly IServiceProvider _coreServices = coreServices;
    private readonly PluginEventRegistry _eventRegistry = new();

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
        foreach (var pluginDll in pluginPaths)
        {
            try
            {
                var context = new PluginLoadContext(pluginDll);
                var assembly = context.LoadFromAssemblyPath(pluginDll);

                var pluginType = assembly.GetTypes()
                    .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract);

                if (pluginType == null) continue;

                var pluginInstance = Activator.CreateInstance(pluginType);
                if (pluginInstance is not IPlugin plugin)
                {
                    _logger.LogError("Failed to create an instance of plugin type: {plugintype}", pluginType.FullName);
                    continue;
                }
                var bootstrapContext = new PluginBootstrapContext
                {
                    Configuration = _configuration,
                    Logger = _logger,
                    CoreServices = _coreServices,
                    PluginDirectory = Path.GetDirectoryName(pluginDll) ?? string.Empty,
                    EventRegistry = _eventRegistry
                };

                plugin.Initialize(bootstrapContext);
                _loadedPlugins.Add((plugin, context));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin: {pluginDll}", pluginDll);
            }
        }
    }

    /// <summary>
    /// Registers all services provided by the loaded plugins into the service collection.
    /// Iterates through each loaded plugin and invokes its <c>RegisterServices</c> method,
    /// allowing the plugin to add its services to the application's dependency injection container.
    /// </summary>
    public void RegisterAllPluginServices()
    {
        foreach (var (plugin, _) in _loadedPlugins)
        {
            plugin.RegisterServices(_serviceCollection);
        }
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

