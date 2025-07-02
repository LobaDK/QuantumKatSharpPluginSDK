using Discord.WebSocket;

namespace QuantumKat.PluginSDK.Core.Interfaces;

public interface IPluginEventRegistry
{
    void SubscribeToMessage(Func<SocketMessage, bool> predicate, Func<SocketMessage, Task> handler);
}