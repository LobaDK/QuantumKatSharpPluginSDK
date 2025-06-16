using Microsoft.Extensions.DependencyInjection;

namespace QuantumKat.PluginSDK;

/// <summary>
/// Represents a plugin for the QuantumKat bot. Assemblies must implement this interface to be recognized as plugins.
/// </summary>
public interface IPlugin
{
    string Name { get; }
    Version Version { get; }
    void Initialize(IServiceCollection services);
}
