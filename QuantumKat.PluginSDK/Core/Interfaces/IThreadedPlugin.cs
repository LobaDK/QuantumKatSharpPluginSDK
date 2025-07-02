namespace QuantumKat.PluginSDK.Core.Interfaces;

/// <summary>
/// Defines an interface for plugins that support asynchronous start and stop operations,
/// typically for plugins that require background threading or long-running tasks.
/// </summary>
public interface IThreadedPlugin : IPlugin
{
    /// <summary>
    /// Starts the plugin asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous start operation.</returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stops the plugin asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    Task StopAsync(CancellationToken cancellationToken);
}