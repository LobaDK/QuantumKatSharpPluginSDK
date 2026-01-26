using QuantumKat.PluginSDK.Core.Interfaces;

namespace Tests.TestPlugins.SimpleTestPlugin;

/// <summary>
/// A simple test plugin that implements the IPlugin interface.
/// Used for testing plugin loading and service registration.
/// </summary>
public class SimpleTestPluginNotImplemented : IPlugin
{
    public string Name => "SimpleTestPluginNotImplemented";
    public string Description => "A simple test plugin for unit testing with no implementations";
    public string Version => "1.0.0";
    public string Author => "Test Suite";
    public Dictionary<string, List<string>> PluginDependencies => [];
}
