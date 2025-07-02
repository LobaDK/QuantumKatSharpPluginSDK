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
                _logger.LogError(ex, $"Failed to load plugin: {pluginDll}");
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
        foreach (var (plugin, _) in _loadedPlugins)
        {
            try
            {
                await plugin.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Plugin '{plugin.Name}' failed to start.");
            }
        }
    }

    public async Task DispatchMessageAsync(SocketMessage message)
    {
        await _eventRegistry.DispatchMessageAsync(message);
    }
}

