using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuantumKat.PluginSDK.Core.Interfaces;

namespace QuantumKat.PluginSDK.Core.Extensions;

/// <summary>
/// Provides extension methods for integrating the Plugin SDK with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Plugin SDK services to the service collection and returns a shared service provider
    /// that can be used to manage plugins and maintain DI consistency between the main application and plugins.
    /// </summary>
    /// <param name="services">The service collection to add Plugin SDK services to.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="logger">The logger instance.</param>
    /// <returns>A <see cref="PluginSDKBuilder"/> that can be used to configure and manage plugins.</returns>
    public static PluginSDKBuilder AddPluginSDK(this IServiceCollection services, IConfiguration configuration, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);

        // Register the shared service provider
        var sharedServiceProvider = new SharedServiceProvider(services);
        services.AddSingleton<IPluginServiceProvider>(sharedServiceProvider);

        // Register the plugin manager
        services.AddSingleton(provider => new PluginManager(
            provider.GetRequiredService<IPluginServiceProvider>(),
            configuration,
            logger));

        return new PluginSDKBuilder(sharedServiceProvider, configuration, logger);
    }
}

/// <summary>
/// Builder for configuring and managing the Plugin SDK.
/// </summary>
public class PluginSDKBuilder(IPluginServiceProvider sharedServiceProvider, IConfiguration configuration, ILogger logger)
{
    private readonly IPluginServiceProvider _sharedServiceProvider = sharedServiceProvider;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger _logger = logger;

    /// <summary>
    /// Gets the shared service provider instance.
    /// </summary>
    public IPluginServiceProvider SharedServiceProvider => _sharedServiceProvider;

    /// <summary>
    /// Loads plugins from the specified paths and registers their services.
    /// </summary>
    /// <param name="pluginPaths">The paths to the plugin assemblies.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public PluginSDKBuilder LoadPlugins(IEnumerable<string> pluginPaths)
    {
        var pluginManager = new PluginManager(_sharedServiceProvider, _configuration, _logger);
        pluginManager.LoadPlugins(pluginPaths);
        pluginManager.RegisterAllPluginServices();
        return this;
    }

    /// <summary>
    /// Builds the final service provider with all registered services from both the main application and plugins.
    /// </summary>
    /// <returns>The built service provider.</returns>
    public IServiceProvider Build()
    {
        return _sharedServiceProvider.ServiceProvider;
    }

    /// <summary>
    /// Configures additional services before building the service provider.
    /// </summary>
    /// <param name="configureServices">An action to configure additional services.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public PluginSDKBuilder ConfigureServices(Action<IServiceCollection> configureServices)
    {
        ArgumentNullException.ThrowIfNull(configureServices);
        
        _sharedServiceProvider.RegisterServicesAndRebuild(configureServices);
        return this;
    }
}