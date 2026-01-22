using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuantumKat.PluginSDK.Core;
using QuantumKat.PluginSDK.Core.Interfaces;

namespace Tests.TestPlugins.SimpleTestPlugin;

/// <summary>
/// A simple test plugin that implements the IPlugin interface.
/// Used for testing plugin loading and service registration.
/// </summary>
public class SimpleTestPlugin : IPlugin
{
    public string Name => "SimpleTestPlugin";
    public string Description => "A simple test plugin for unit testing";
    public string Version => "1.0.0";
    public string Author => "Test Suite";
    public Dictionary<string, List<string>> PluginDependencies => [];

    public void Initialize(PluginBootstrapContext context)
    {
        // Simple initialization - no special setup needed

        ConfigurationBuilder configurationBuilder = context.CoreServices.GetRequiredService<ConfigurationBuilder>();
        configurationBuilder.AddJsonFile(Path.Combine(context.PluginDirectory, "TestPlugins/SimpleTestPlugin/", "simpletestpluginsettings.json"), optional: false, reloadOnChange: false);
    }

    public void RegisterServices(IServiceCollection services)
    {
        // Register a marker service to verify this plugin was loaded
        services.AddSingleton<SimpleTestPluginMarker>();
    }
}

/// <summary>
/// Marker class to verify SimpleTestPlugin was loaded and registered.
/// </summary>
public class SimpleTestPluginMarker { }
