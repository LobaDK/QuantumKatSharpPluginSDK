using Microsoft.Extensions.DependencyInjection;

namespace QuantumKat.PluginSDK.Core.Interfaces;

/// <summary>
/// Defines the contract for a plugin within the QuantumKat Plugin SDK.
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Gets the name of the plugin.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of the plugin.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the version of the plugin.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the author of the plugin.
    /// </summary>
    string Author { get; }

    /// <summary>
    /// Initializes the plugin with the specified bootstrap context.
    /// </summary>
    /// <param name="context">The context used to initialize the plugin.</param>
    void Initialize(PluginBootstrapContext context);

    /// <summary>
    /// Registers the plugin's services with the provided service collection.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    void RegisterServices(IServiceCollection services);
}