using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuantumKat.PluginSDK.Core.Interfaces;

namespace QuantumKat.PluginSDK.Core;

public class PluginManager(IServiceCollection services, IConfiguration configuration, ILogger logger, IServiceProvider coreServices)
{
    private readonly List<(IPlugin plugin, PluginLoadContext context)> _loadedPlugins = [];
    private readonly IServiceCollection _serviceCollection = services;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger _logger = logger;
    private readonly IServiceProvider _coreServices = coreServices;
    private readonly PluginEventRegistry _eventRegistry = new();

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

    public void RegisterAllPluginServices()
    {
        foreach (var (plugin, _) in _loadedPlugins)
        {
            plugin.RegisterServices(_serviceCollection);
        }
    }

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

    public async Task DispatchMessageAsync(SocketMessage message)
    {
        await _eventRegistry.DispatchMessageAsync(message);
    }
}

