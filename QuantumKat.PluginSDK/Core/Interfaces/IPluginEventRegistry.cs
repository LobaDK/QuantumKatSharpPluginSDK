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
    /// <param name="name"
    /// >A unique name for the subscription.
    /// </param>
    /// <param name="predicate">
    /// An asynchronous function that determines whether a given <see cref="SocketMessage"/> should trigger the handler.
    /// </param>
    /// <param name="handler">
    /// An asynchronous function to handle the <see cref="SocketMessage"/> when the predicate returns <c>true</c>.
    /// </param>
    void SubscribeToMessage(string name, Func<SocketMessage, Task<bool>> predicate, Func<SocketMessage, Task> handler);


    /// <summary>
    /// Unsubscribes from the message event with the specified name.
    /// </summary>
    /// <param name="name">
    /// The unique name of the subscription to remove.
    /// </param>
    void UnsubscribeFromMessage(string name);

    /// <summary>
    /// Clears all registered message subscriptions.
    /// </summary>
    void ClearAllSubscriptions();

    /// <summary>
    /// Determines whether a subscription with the specified name exists.
    /// </summary>
    /// <param name="name">
    /// The unique name of the subscription to check.
    /// </param>
    /// <returns>
    /// <c>true</c> if a subscription with the specified name exists; otherwise, <c>false</c>.
    /// </returns>
    bool IsSubscribed(string name);

    /// <summary>
    /// Gets a snapshot of all registered subscriptions for the calling assembly.
    /// </summary>
    /// <returns>
    /// A dictionary containing the names and corresponding predicate-handler pairs of all registered subscriptions.
    /// </returns>
    Dictionary<string, (Func<SocketMessage, Task<bool>> predicate, Func<SocketMessage, Task> handler)> GetSubscriptions();
}