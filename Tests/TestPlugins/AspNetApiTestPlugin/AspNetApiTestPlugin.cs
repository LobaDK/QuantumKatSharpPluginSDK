using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using QuantumKat.PluginSDK.Core;
using QuantumKat.PluginSDK.Core.Interfaces;

namespace Tests.TestPlugins.AspNetApiTestPlugin;

/// <summary>
/// A threaded test plugin that runs an ASP.NET Core API server.
/// Used for testing async plugin lifecycle operations with a real HTTP server.
/// </summary>
public class AspNetApiTestPlugin : IThreadedPlugin
{
    private WebApplication? _app;
    private bool _isRunning;
    private CancellationToken _cancellationToken;

    public string Name => "AspNetApiTestPlugin";
    public string Description => "A threaded test plugin that runs an ASP.NET API server";
    public string Version => "1.0.0";
    public string Author => "Test Suite";
    public Dictionary<string, List<string>> PluginDependencies => [];

    /// <summary>
    /// Gets the port the server is running on. Defaults to 5555.
    /// </summary>
    public int ServerPort { get; set; } = 5555;

    public void Initialize(PluginBootstrapContext context)
    {
        // Initialize the plugin
    }

    public void RegisterServices(IServiceCollection services)
    {
        // Register a marker service to verify this plugin was loaded
        services.AddSingleton<AspNetApiTestPluginMarker>();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;

        // Create builder and app
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = [$"--urls=http://localhost:{ServerPort}"]
        });

        var app = builder.Build();

        // Configure endpoints
        app.MapGet("/api/health", () => new { status = "healthy", message = "AspNetApiTestPlugin is running" })
            .WithName("HealthCheck");

        app.MapGet("/api/test", () => new { message = "Test endpoint works!" })
            .WithName("TestEndpoint");

        _app = app;

        // Start the app
        await _app.StartAsync(cancellationToken);
        _isRunning = true;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _isRunning = false;
        
        if (_app != null)
        {
            await _app.StopAsync(cancellationToken);
            await _app.DisposeAsync();
            _app = null;
        }
    }

    public bool IsRunning => _isRunning;
}

/// <summary>
/// Marker class to verify AspNetApiTestPlugin was loaded and registered.
/// </summary>
public class AspNetApiTestPluginMarker { }
