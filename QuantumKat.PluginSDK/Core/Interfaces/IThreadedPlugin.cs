namespace QuantumKat.PluginSDK.Core.Interfaces;

public interface IThreadedPlugin : IPlugin
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}