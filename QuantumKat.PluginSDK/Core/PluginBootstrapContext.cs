using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuantumKat.PluginSDK.Core.Interfaces;

namespace QuantumKat.PluginSDK.Core;

/// <summary>
/// Provides contextual information and services required for bootstrapping a plugin.
/// </summary>
///
/// <remarks>
/// This context is typically passed to plugins during initialization to provide access to configuration,
/// logging, core services, and plugin-specific resources.
/// </remarks>
public class PluginBootstrapContext
{
    /// <summary>
    /// Gets the configuration settings for the plugin.
    /// </summary>
    public required IConfiguration Configuration { get; init; }

    /// <summary>
    /// Gets the logger instance for logging plugin-related information and errors.
    /// </summary>
    public required ILogger Logger { get; init; }

    /// <summary>
    /// Gets the service provider for accessing core application services.
    /// </summary>
    public required IServiceProvider CoreServices { get; init; }

    /// <summary>
    /// Gets the shared service provider that can be used to register additional services
    /// and rebuild the service provider to maintain consistency between the main application and plugins.
    /// </summary>
    public IPluginServiceProvider? SharedServiceProvider { get; init; }

    /// <summary>
    /// Gets the directory path where the plugin is located.
    /// </summary>
    public required string PluginDirectory { get; init; }

    /// <summary>
    /// Gets the event registry for registering and handling plugin events, if available.
    /// </summary>
    public IPluginEventRegistry? EventRegistry { get; init; }
}