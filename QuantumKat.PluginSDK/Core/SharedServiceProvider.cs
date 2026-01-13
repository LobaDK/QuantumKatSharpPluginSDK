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
    private readonly object _lock = new();

    /// <inheritdoc />
    public IServiceProvider ServiceProvider
    {
        get
        {

            lock (_lock)
            {
                ObjectDisposedException.ThrowIf(_disposed, nameof(SharedServiceProvider));

                _serviceProvider ??= _serviceCollection.BuildServiceProvider(new ServiceProviderOptions{ValidateOnBuild = true, ValidateScopes = true});

                return _serviceProvider;
            }
        }
    }

    /// <inheritdoc />
    public void RebuildServiceProvider()
    {
        lock (_lock)
        {
            RefreshServiceProvider();
        }
    }

    /// <inheritdoc />
    public void RegisterServicesAndRebuild(Action<IServiceCollection> serviceRegistration)
    {
        ArgumentNullException.ThrowIfNull(serviceRegistration);
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(SharedServiceProvider));

            serviceRegistration(_serviceCollection);
            RefreshServiceProvider();
        }
    }

    /// <summary>
    /// Disposes the current service provider if it implements <see cref="IDisposable"/>.
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;

            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
                _serviceProvider = null;
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    private void RefreshServiceProvider()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(SharedServiceProvider));

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
            _serviceProvider = null;
        }

        _serviceProvider = _serviceCollection.BuildServiceProvider(new ServiceProviderOptions{ValidateOnBuild = true, ValidateScopes = true});
    }
}
