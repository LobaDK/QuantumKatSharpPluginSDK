using Microsoft.Extensions.DependencyInjection;
using QuantumKat.PluginSDK.Core;
using QuantumKat.PluginSDK.Core.Interfaces;

namespace Tests.TestPlugins.ThreadedTestPlugin;

/// <summary>
/// A threaded test plugin that implements the IThreadedPlugin interface.
/// Used for testing async plugin lifecycle operations.
/// </summary>
public class ThreadedTestPlugin : IThreadedPlugin
{
    public bool _isRunning = false;

    public string Name => "ThreadedTestPlugin";
    public string Description => "A threaded test plugin for async testing";
    public string Version => "2.0.0";
    public string Author => "Test Suite";
    public Dictionary<string, List<string>> PluginDependencies => [];

    public void Initialize(PluginBootstrapContext context)
    {
        // Initialize the threaded plugin
    }

    public void RegisterServices(IServiceCollection services)
    {
        // Register a marker service to verify this plugin was loaded
        services.AddSingleton<ThreadedTestPluginMarker>();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _isRunning = true;
        
        // Simulate some async startup work
        await Task.Delay(10, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _isRunning = false;
        
        // Simulate some async shutdown work
        await Task.Delay(10, cancellationToken);
    }

    public bool IsRunning => _isRunning;
}

/// <summary>
/// Marker class to verify ThreadedTestPlugin was loaded and registered.
/// </summary>
public class ThreadedTestPluginMarker { }
