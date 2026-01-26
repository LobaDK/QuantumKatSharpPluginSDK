using Discord;
using Discord.WebSocket;

namespace QuantumKat.PluginSDK.Discord.Extensions;


/// <summary>
/// Provides extension methods for <see cref="IMessage"/>.
/// </summary>
public static class IMessageExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="IMessage"/> is a user message.
    /// </summary>
    /// <param name="message">The <see cref="IMessage"/> to check.</param>
    /// <returns>
    /// <c>true</c> if the <paramref name="message"/> is a <see cref="SocketUserMessage"/>; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsUserMessage(this IMessage message, out SocketUserMessage? userMessage)
    {
        if (message is SocketUserMessage userMsg)
        {
            userMessage = userMsg;
            return true;
        }

        userMessage = null;
        return false;
    }

    /// <summary>
    /// Determines whether the specified <see cref="IMessage"/> is a bot message.
    /// </summary>
    /// <param name="message">The <see cref="IMessage"/> to check.</param>
    /// <returns>
    /// <c>true</c> if the <paramref name="message"/> is a bot message; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsFromBot(this IMessage message)
    {
        return message.Author.IsBot;
    }
}
