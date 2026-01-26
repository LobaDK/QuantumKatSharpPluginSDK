using System.Collections.Concurrent;
using System.Reflection;
using Discord;
using QuantumKat.PluginSDK.Core.Interfaces;

namespace QuantumKat.PluginSDK.Core;

/// <summary>
/// A registry for subscribing to plugin message events.
/// </summary>
/// <remarks>
/// This registry allows plugins to register handlers for specific <see cref="IMessage"/> events
/// based on a predicate filter.
/// </remarks>
public class PluginEventRegistry : IPluginEventRegistry
{
    /// <summary>
    /// Holds the registered message handlers along with their predicates.
    /// </summary>
    /// <remarks>
    /// The key is the assembly name of the plugin, and the value is a collection of tuples containing:
    /// - The name of the handler.
    /// - The predicate function to filter messages.
    /// - The handler function to process the message.
    /// </remarks>
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, (Func<IMessage, Task<bool>> predicate, Func<IMessage, Task> handler)>> _messageHandlers = [];

    /// <summary>
    /// Subscribes a handler to process <see cref="IMessage"/> instances that match a specified predicate.
    /// </summary>
    /// <param name="name">
    /// A unique identifier for this subscription within the calling assembly. Used for unsubscribing.
    /// </param>
    /// <param name="predicate">
    /// A asynchronous function that determines whether a given <see cref="IMessage"/> should be handled.
    /// Returns <c>true</c> if the message should be handled; otherwise, <c>false</c>.
    /// This function is called for every message dispatched through the registry.
    /// </param>
    /// <param name="handler">
    /// An asynchronous function to handle the <see cref="IMessage"/> when the predicate returns <c>true</c>.
    /// Any exceptions thrown by this handler will be caught and logged to the console.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a subscription with the same name already exists for the calling assembly.
    /// </exception>
    /// <remarks>
    /// Each plugin (assembly) maintains its own namespace of subscription names.
    /// The same name can be used by different assemblies without conflict.
    /// </remarks>
    public void SubscribeToMessage(string name, Func<IMessage, Task<bool>> predicate, Func<IMessage, Task> handler)
    {
        Assembly assembly = Assembly.GetCallingAssembly();
        string assemblyName = assembly.ToString();

        (Func<IMessage, Task<bool>>, Func<IMessage, Task>) newHandler = (predicate, handler);
        var handlersDict = _messageHandlers.GetOrAdd(
            assemblyName,
            _ => new ConcurrentDictionary<string, (Func<IMessage, Task<bool>>, Func<IMessage, Task>)>());

        if (!handlersDict.TryAdd(name, newHandler))
        {
            throw new InvalidOperationException($"Subscription '{name}' already exists.");
        }
    }

    /// <summary>
    /// Asynchronously dispatches a <see cref="IMessage"/> to all registered message handlers whose predicates match the message.
    /// </summary>
    /// <param name="message">The <see cref="IMessage"/> to be dispatched to handlers.</param>
    /// <returns>A task that represents the asynchronous dispatch operation.</returns>
    public async Task DispatchMessageAsync(IMessage message)
    {
        foreach (var handlers in _messageHandlers.Values.ToList())
        {
            foreach (KeyValuePair<string, (Func<IMessage, Task<bool>>, Func<IMessage, Task>)> handler in handlers)
            {
                try
                {
                    var (predicate, handlerFunc) = handler.Value;
                    if (await predicate(message))
                        await handlerFunc(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in plugin handler '{handler.Key}': {ex}");
                }
            }
        }
    }

    /// <summary>
    /// Unsubscribes a previously registered message handler by name.
    /// </summary>
    /// <param name="name">
    /// The name of the subscription to remove. This should match the name used when calling <see cref="SubscribeToMessage"/>.
    /// </param>
    /// <remarks>
    /// This method removes the handler registration for the calling assembly only.
    /// If no subscription with the specified name exists, the method does nothing.
    /// </remarks>
    public void UnsubscribeFromMessage(string name)
    {
        Assembly assembly = Assembly.GetCallingAssembly();
        string assemblyName = assembly.ToString();

        if (_messageHandlers.TryGetValue(assemblyName, out var values) && values is not null)
        {
            KeyValuePair<string, (Func<IMessage, Task<bool>>, Func<IMessage, Task>)> toRemove = values.FirstOrDefault(v => v.Key == name);
            if (toRemove.Key != null)
            {
                values.TryRemove(toRemove.Key, out _);
            }
        }
    }

    /// <summary>
    /// Removes all message handler subscriptions for the calling assembly.
    /// </summary>
    /// <remarks>
    /// This method clears all registered handlers for the calling plugin/assembly,
    /// effectively unsubscribing from all message events. Handlers from other assemblies remain unaffected.
    /// This is useful for plugin cleanup or when resetting all subscriptions.
    /// </remarks>
    public void ClearAllSubscriptions()
    {
        Assembly assembly = Assembly.GetCallingAssembly();
        string assemblyName = assembly.ToString();

        if (_messageHandlers.TryGetValue(assemblyName, out var values) && values is not null)
        {
            _messageHandlers.TryRemove(assemblyName, out _);
        }
    }

    /// <summary>
    /// Determines whether a subscription with the specified name exists for the calling assembly.
    /// </summary>
    /// <param name="name">The name of the subscription to check for.</param>
    /// <returns>
    /// <c>true</c> if a subscription with the specified name exists for the calling assembly; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method only checks subscriptions within the calling assembly's namespace.
    /// Subscriptions from other assemblies are not considered.
    /// </remarks>
    public bool IsSubscribed(string name)
    {
        Assembly assembly = Assembly.GetCallingAssembly();
        string assemblyName = assembly.ToString();

        if (_messageHandlers.TryGetValue(assemblyName, out var values) && values is not null)
        {
            return values.ContainsKey(name);
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Retrieves all message handler subscriptions for the calling assembly.
    /// </summary>
    /// <returns>
    /// A dictionary containing all subscriptions for the calling assembly, where:
    /// <list type="bullet"> 
    ///     <item>
    ///         <term>Key</term>
    ///         <description>The subscription name.</description>
    ///     </item>
    ///     <item>
    ///        <term>Value</term>
    ///       <description>The tuple of predicate and handler functions.</description>
    ///    </item>
    /// </list>
    /// Returns an empty dictionary if no subscriptions exist for the calling assembly.
    /// </returns>
    /// <remarks>
    /// This method only returns subscriptions registered by the calling assembly.
    /// Subscriptions from other assemblies are not included in the result.
    /// The returned dictionary is a snapshot and modifications to it will not affect the registry.
    /// </remarks>
    public Dictionary<string, (Func<IMessage, Task<bool>> predicate, Func<IMessage, Task> handler)> GetSubscriptions()
    {
        Assembly assembly = Assembly.GetCallingAssembly();
        string assemblyName = assembly.ToString();

        if (_messageHandlers.TryGetValue(assemblyName, out var values) && values is not null)
        {
            return values.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        else
        {
            return [];
        }
    } 
}