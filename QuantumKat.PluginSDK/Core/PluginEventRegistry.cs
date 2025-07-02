using Discord.WebSocket;
using QuantumKat.PluginSDK.Core.Interfaces;

namespace QuantumKat.PluginSDK.Core;

public class PluginEventRegistry : IPluginEventRegistry
{
    private readonly List<(Func<SocketMessage, bool> predicate, Func<SocketMessage, Task> handler)> _messageHandlers = new();

    public void SubscribeToMessage(Func<SocketMessage, bool> predicate, Func<SocketMessage, Task> handler)
    {
        _messageHandlers.Add((predicate, handler));
    }

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