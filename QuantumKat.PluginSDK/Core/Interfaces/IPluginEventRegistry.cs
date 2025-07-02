using Discord.WebSocket;

namespace QuantumKat.PluginSDK.Core.Interfaces;

/// <summary>
/// Defines a registry for subscribing to plugin message events.
/// </summary>
///
/// <remarks>
/// Implementations of this interface allow plugins to register handlers for specific <see cref="SocketMessage"/> events
/// based on a predicate filter.
/// </remarks>
public interface IPluginEventRegistry
{
    /// <summary>
    /// Subscribes to incoming <see cref="SocketMessage"/> events that match the specified predicate.
    /// </summary>
    /// <param name="predicate">
    /// A function that determines whether a given <see cref="SocketMessage"/> should trigger the handler.
    /// </param>
    /// <param name="handler">
    /// An asynchronous function to handle the <see cref="SocketMessage"/> when the predicate returns <c>true</c>.
    /// </param>
    void SubscribeToMessage(Func<SocketMessage, bool> predicate, Func<SocketMessage, Task> handler);
}