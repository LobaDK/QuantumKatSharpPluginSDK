using Microsoft.Extensions.DependencyInjection;

namespace QuantumKat.PluginSDK.Core.Interfaces;

public interface IPlugin
{
    string Name { get; }
    string Description { get; }
    string Version { get; }
    string Author { get; }
    void Initialize(PluginBootstrapContext context);
    void RegisterServices(IServiceCollection services);
}