namespace QuantumKat.PluginSDK;

public interface IThreaded
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    bool IsRunning { get; }
}
