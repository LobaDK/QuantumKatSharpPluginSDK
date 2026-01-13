using Discord.WebSocket;

namespace QuantumKat.PluginSDK.Core.Interfaces;

/// <summary>
/// Defines the contract for managing the loading, initialization, and lifecycle of plugins.
/// </summary>
public interface IPluginManager
{
    /// <summary>
    /// Loads and initializes plugins from the specified collection of plugin assembly file paths.
    /// </summary>
    /// <param name="pluginPaths">
    /// An <see cref="IEnumerable{String}"/> containing the file paths to the plugin assemblies to load.
    /// </param>
    void LoadPlugins(IEnumerable<string> pluginPaths);

    /// <summary>
    /// Registers all services provided by the loaded plugins into the shared service provider.
    /// </summary>
    void RegisterAllPluginServices();

    /// <summary>
    /// Asynchronously starts all loaded plugins that implement <see cref="IThreadedPlugin"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation of starting all threaded plugins.</returns>
    Task StartAllPluginsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously stops all loaded plugins that implement <see cref="IThreadedPlugin"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation of stopping all threaded plugins.</returns>
    Task StopAllPluginsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously starts a specific plugin that implements <see cref="IThreadedPlugin"/>.
    /// </summary>
    /// <param name="plugin">The plugin to start.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task StartPluginAsync(IThreadedPlugin plugin, CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously stops a specific plugin that implements <see cref="IThreadedPlugin"/>.
    /// </summary>
    /// <param name="plugin">The plugin to stop.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task StopPluginAsync(IThreadedPlugin plugin, CancellationToken cancellationToken);

    /// <summary>
    /// Dispatches a message to all registered plugin handlers.
    /// </summary>
    /// <param name="message">The <see cref="SocketMessage"/> to dispatch.</param>
    Task DispatchMessageAsync(SocketMessage message);
}
