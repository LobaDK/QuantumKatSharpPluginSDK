using Microsoft.Extensions.DependencyInjection;
using QuantumKat.PluginSDK.Core.Interfaces;

namespace QuantumKat.PluginSDK.Core;

/// <summary>
/// Manages a shared service provider that can be rebuilt as plugins register new services.
/// This ensures that both the main application and plugins share the same DI container.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SharedServiceProvider"/> class.
/// </remarks>
/// <param name="serviceCollection">The service collection to build the service provider from.</param>
public class SharedServiceProvider(IServiceCollection serviceCollection) : IPluginServiceProvider, IDisposable
{
    private readonly IServiceCollection _serviceCollection = serviceCollection ?? throw new ArgumentNullException(nameof(serviceCollection));
    private IServiceProvider? _serviceProvider;
    private bool _disposed;

    /// <inheritdoc />
    public IServiceProvider ServiceProvider
    {
        get
        {
            return _disposed
                ? throw new ObjectDisposedException(nameof(SharedServiceProvider))
                : (_serviceProvider ??= _serviceCollection.BuildServiceProvider());
        }
    }

    /// <inheritdoc />
    public void RebuildServiceProvider()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(SharedServiceProvider));
        
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        
        _serviceProvider = _serviceCollection.BuildServiceProvider();
    }

    /// <inheritdoc />
    public void RegisterServicesAndRebuild(Action<IServiceCollection> serviceRegistration)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(SharedServiceProvider));

        ArgumentNullException.ThrowIfNull(serviceRegistration);
        
        serviceRegistration(_serviceCollection);
        RebuildServiceProvider();
    }

    /// <summary>
    /// Disposes the current service provider if it implements <see cref="IDisposable"/>.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}