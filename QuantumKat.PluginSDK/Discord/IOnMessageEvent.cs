using Discord.WebSocket;

namespace QuantumKat.PluginSDK.Discord;

/// <summary>
/// Marks a class as capable of handling Discord message events.
/// </summary>
public interface IOnMessageEvent
{
    /// <summary>
    /// Handles an incoming Discord message asynchronously.
    /// </summary>
    /// <param name="socketMessage">The message received from the Discord socket.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// This method is called when a message is received from the Discord socket. Implementations should
    /// provide the logic to handle the message, such as processing commands or responding to user input.
    /// No checks are performed to ensure that the message is valid or relevant to the handler, so implementers should
    /// ensure that they are handling the correct type of message.
    /// </remarks>
    /// <example>
    /// <code>
    /// public class MyMessageHandler : IOnMessageEvent
    /// {
    ///     public async Task HandleMessageAsync(SocketMessage socketMessage)
    ///     {
    ///         // Handle the message here
    ///         if (socketMessage is SocketUserMessage userMessage)
    ///         {
    ///             // Process the user message
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="SocketMessage"/>
    /// <seealso cref="SocketUserMessage"/>
    Task HandleMessageAsync(SocketMessage socketMessage);
}
