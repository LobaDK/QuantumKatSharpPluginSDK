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
    /// Gets the dependencies of the plugin, where the key is the plugin name and the value is a list of acceptable version strings.
    /// </summary>
    /// <remarks>
    /// <c>&lt;</c>, <c>&gt;</c> and <c>=</c> may be used like standard C# comparison operators to specify version requirements.
    /// </remarks>
    Dictionary<string, List<string>> PluginDependencies { get; }

    /// <summary>
    /// Initializes the plugin with the specified bootstrap context.
    /// </summary>
    /// <param name="context">The context used to initialize the plugin.</param>
    virtual void Initialize(PluginBootstrapContext context)
    {
        return;
    }

    /// <summary>
    /// Registers the plugin's services with the provided service collection.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    virtual void RegisterServices(IServiceCollection services)
    {
        return;
    }
}