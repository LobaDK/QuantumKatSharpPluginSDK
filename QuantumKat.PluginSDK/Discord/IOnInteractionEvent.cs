using Discord.WebSocket;

namespace QuantumKat.PluginSDK.Discord;

/// <summary>
/// Marks a class as capable of handling Discord interaction events.
/// </summary>
public interface IOnInteractionEvent
{
    /// <summary>
    /// Handles a Discord interaction event asynchronously.
    /// </summary>
    /// <param name="socketInteraction">The interaction object containing details about the event.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// This method is called when a Discord interaction event occurs. Implementations should
    /// provide the logic to handle the event, such as responding to the interaction or processing
    /// the data contained in the interaction. No checks are performed to ensure that the
    /// interaction is valid or relevant to the handler, so implementers should ensure that they
    /// are handling the correct type of interaction.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when the socketInteraction parameter is null.</exception>
    /// <example>
    /// <code>
    /// public class MyInteractionHandler : IOnInteractionEvent
    /// {
    ///     public async Task HandleInteractionAsync(SocketInteraction socketInteraction)
    ///     {
    ///         // Handle the interaction here
    ///         if (socketInteraction is SocketSlashCommand command)
    ///         {
    ///             // Process the slash command
    ///             await command.RespondAsync("Command received!");
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="SocketInteraction"/>
    /// <seealso cref="SocketSlashCommand"/>
    Task HandleInteractionAsync(SocketInteraction socketInteraction);
}
