using Microsoft.Extensions.DependencyInjection;

namespace QuantumKat.PluginSDK.Core.Interfaces;

/// <summary>
/// Defines the contract for managing shared services between the main application and plugins.
/// </summary>
public interface IPluginServiceProvider
{
    /// <summary>
    /// Gets the current service provider instance.
    /// </summary>
    /// <remarks>
    /// This provider is shared between the main application and all loaded plugins,
    /// ensuring consistent dependency injection throughout the system.
    /// </remarks>
    IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Rebuilds the service provider with all currently registered services.
    /// </summary>
    /// <remarks>
    /// This method should be called after plugins have registered their services
    /// to ensure all services are available in the shared service provider.
    /// </remarks>
    void RebuildServiceProvider();

    /// <summary>
    /// Registers services and rebuilds the service provider.
    /// </summary>
    /// <param name="serviceRegistration">An action that registers services with the service collection.</param>
    void RegisterServicesAndRebuild(Action<IServiceCollection> serviceRegistration);
}