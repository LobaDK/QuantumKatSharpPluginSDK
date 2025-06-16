using Discord.WebSocket;

namespace QuantumKat.PluginSDK.Discord.Extensions;


/// <summary>
/// Provides extension methods for <see cref="SocketMessage"/>.
/// </summary>
public static class SocketMessageExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="SocketMessage"/> is a user message.
    /// </summary>
    /// <param name="socketMessage">The <see cref="SocketMessage"/> to check.</param>
    /// <returns>
    /// <c>true</c> if the <paramref name="socketMessage"/> is a <see cref="SocketUserMessage"/>; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsUserMessage(this SocketMessage socketMessage, out SocketUserMessage userMessage)
    {
        if (socketMessage is SocketUserMessage userMsg)
        {
            userMessage = userMsg;
            return true;
        }

        userMessage = null;
        return false;
    }

    /// <summary>
    /// Determines whether the specified <see cref="SocketMessage"/> is a bot message.
    /// </summary>
    /// <param name="socketMessage">The <see cref="SocketMessage"/> to check.</param>
    /// <returns>
    /// <c>true</c> if the <paramref name="socketMessage"/> is a bot message; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsFromBot(this SocketMessage socketMessage)
    {
        return socketMessage.Author.IsBot;
    }
}
