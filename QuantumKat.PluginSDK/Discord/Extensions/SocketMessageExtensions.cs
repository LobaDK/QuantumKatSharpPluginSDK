using Discord;

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
    /// <c>true</c> if the <paramref name="message"/> is a <see cref="IUserMessage"/>; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsUserMessage(this IMessage message, out IUserMessage? userMessage)
    {
        if (message is IUserMessage userMsg)
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
