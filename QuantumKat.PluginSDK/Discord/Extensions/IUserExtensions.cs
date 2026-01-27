using Discord;
using Discord.WebSocket;

namespace QuantumKat.PluginSDK.Discord.Extensions;

public static class IUserExtensions
{
    private static IDiscordClient? _client;

    public static void Initialize(IDiscordClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Checks if the user is the client (bot) itself by comparing their IDs.
    /// </summary>
    /// <param name="user">The <c>IUser</c> object representing the user.</param>
    /// <returns>Boolean indicating whether or not the user is the client.</returns>
    public static bool IsClient(this IUser user)
    {
        if (_client == null)
        {
            throw new InvalidOperationException("DiscordSocketClient has not been initialized.");
        }

        return user.Id == _client.CurrentUser.Id;
    }
}
