using Discord.WebSocket;
using QuantumKat.PluginSDK.Core.Interfaces;

namespace QuantumKat.PluginSDK.Core;

/// <summary>
/// A registry for subscribing to plugin message events.
/// </summary>
/// <remarks>
/// This registry allows plugins to register handlers for specific <see cref="SocketMessage"/> events
/// based on a predicate filter.
/// </remarks>
public class PluginEventRegistry : IPluginEventRegistry
{
    private readonly List<(Func<SocketMessage, bool> predicate, Func<SocketMessage, Task> handler)> _messageHandlers = new();

    /// <summary>
    /// Subscribes a handler to process <see cref="SocketMessage"/> instances that match a specified predicate.
    /// </summary>
    /// <param name="predicate">
    /// A function that determines whether a given <see cref="SocketMessage"/> should be handled.
    /// Returns <c>true</c> if the message should be handled; otherwise, <c>false</c>.
    /// </param>
    /// <param name="handler">
    /// An asynchronous function to handle the <see cref="SocketMessage"/> when the predicate returns <c>true</c>.
    /// </param>
    public void SubscribeToMessage(Func<SocketMessage, bool> predicate, Func<SocketMessage, Task> handler)
    {
        _messageHandlers.Add((predicate, handler));
    }

    /// <summary>
    /// Asynchronously dispatches a <see cref="SocketMessage"/> to all registered message handlers whose predicates match the message.
    /// </summary>
    /// <param name="message">The <see cref="SocketMessage"/> to be dispatched to handlers.</param>
    /// <returns>A task that represents the asynchronous dispatch operation.</returns>
    public async Task DispatchMessageAsync(SocketMessage message)
    {
        foreach (var (predicate, handler) in _messageHandlers)
        {
            try
            {
                if (predicate(message))
                    await handler(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in plugin handler: {ex.Message}");
            }
        }
    }
}