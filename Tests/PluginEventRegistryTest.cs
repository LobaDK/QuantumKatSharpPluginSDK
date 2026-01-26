using Discord;
using Moq;
using QuantumKat.PluginSDK.Core;

namespace Tests;
 
public class PluginEventRegistryTest
{
    [Fact]
    public void RegisterEvent_ShouldAddEventToRegistry()
    {
        // Arrange
        var registry = new PluginEventRegistry();
        var eventName = "TestEvent";
        static Task<bool> predicate(IMessage _) => Task.FromResult(true);
        static Task handler(IMessage _) => Task.CompletedTask;
        
        // Act
        registry.SubscribeToMessage(eventName, predicate, handler);

        // Assert
        Assert.True(registry.IsSubscribed(eventName));
    }

    [Fact]
    public void RegisterEvent_ShouldThrowOnDuplicateRegistration()
    {
        // Arrange
        var registry = new PluginEventRegistry();
        var eventName = "TestEvent";
        static Task<bool> predicate(IMessage _) => Task.FromResult(true);
        static Task handler(IMessage _) => Task.CompletedTask;
        registry.SubscribeToMessage(eventName, predicate, handler);
        
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => registry.SubscribeToMessage(eventName, predicate, handler));
    }

    [Fact]
    public void UnregisterEvent_ShouldRemoveEventFromRegistry()
    {
        // Arrange
        var registry = new PluginEventRegistry();
        var eventName = "TestEvent";
        static Task<bool> predicate(IMessage _) => Task.FromResult(true);
        static Task handler(IMessage _) => Task.CompletedTask;
        registry.SubscribeToMessage(eventName, predicate, handler);
        
        // Act
        registry.UnsubscribeFromMessage(eventName);
        
        // Assert
        Assert.False(registry.IsSubscribed(eventName));
    }

    [Fact]
    public void IsEventRegistered_ShouldReturnFalseForUnregisteredEvent()
    {
        // Arrange
        var registry = new PluginEventRegistry();
        var eventName = "NonExistentEvent";
        
        // Act & Assert
        Assert.False(registry.IsSubscribed(eventName));
    }

    [Fact]
    public void GetRegisteredEvents_ShouldReturnAllRegisteredEvents()
    {
        // Arrange
        var registry = new PluginEventRegistry();
        var event1 = "Event1";
        var event2 = "Event2";
        static Task<bool> predicate(IMessage _) => Task.FromResult(true);
        static Task handler(IMessage _) => Task.CompletedTask;
        registry.SubscribeToMessage(event1, predicate, handler);
        registry.SubscribeToMessage(event2, predicate, handler);
        
        // Act
        var events = registry.GetSubscriptions();
        
        // Assert
        Assert.Contains(event1, events);
        Assert.Contains(event2, events);
        Assert.Equal(2, events.Count);
    }

    [Fact]
    public async Task RegisteredEvents_CanInvoke()
    {
        // Arrange
        var registry = new PluginEventRegistry();
        
        var event1 = "Event1";
        var response = "";
        
        static Task<bool> predicate(IMessage message) => Task.FromResult(message.Content == "Hello!");
        Task handler(IMessage _) => Task.Run(() => response = "Handler Invoked");
        
        registry.SubscribeToMessage(event1, predicate, handler);

        Mock<IMessage> mockMessage = new();
        mockMessage.SetupGet(m => m.Content).Returns("Hello!");

        // Act
        await registry.DispatchMessageAsync(mockMessage.Object);

        // Assert
        Assert.Equal("Handler Invoked", response);
    }
}