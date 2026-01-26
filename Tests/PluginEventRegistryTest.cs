using Discord;
using Discord.WebSocket;
using Moq;
using QuantumKat.PluginSDK.Core;
using QuantumKat.PluginSDK.Discord.Extensions;

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

    [Fact]
    public async Task RegisteredEvents_OnlyMatchingInvoke()
    {
        // Arrange
        var registry = new PluginEventRegistry();
        
        var event1 = "Event1";
        var response = "";
        
        static Task<bool> predicate(IMessage message) => Task.FromResult(message.Content == "Hello!");
        Task handler(IMessage _) => Task.Run(() => response = "Handler Invoked");
        
        registry.SubscribeToMessage(event1, predicate, handler);

        Mock<IMessage> mockMessage = new();
        mockMessage.SetupGet(m => m.Content).Returns("Goodbye!");

        // Act
        await registry.DispatchMessageAsync(mockMessage.Object);

        // Assert
        Assert.Equal("", response);
    }

    [Fact]
    public async Task RegisteredEvents_SupportsAdvancedPredicates()
    {
        // Arrange
        var registry = new PluginEventRegistry();
        
        var event1 = "DoesMessageContainSecret";
        var response1 = "";

        var event2 = "IsMessageValidCommand";
        var response2 = "";
        
        static Task<bool> predicate(IMessage message) => Task.FromResult(message.Content.Contains("Secret"));
        Task handler1(IMessage _) => Task.Run(() => response1 = "Handler1 Invoked");
        Task handler2(IMessage _) => Task.Run(() => response2 = "Handler2 Invoked");
        
        registry.SubscribeToMessage(event1, predicate, handler1);
        registry.SubscribeToMessage(event2, AdvancedPredicate, handler2);

        Mock<IMessage> mockMessage1 = new();
        mockMessage1.SetupGet(m => m.Content).Returns("This is a Secret Message!");
        mockMessage1.SetupGet(m => m.Author).Returns(new Mock<IUser>().Object);
        mockMessage1.SetupGet(m => m.Author.IsBot).Returns(true);

        Mock<IUserMessage> mockMessage2 = new();
        mockMessage2.SetupGet(m => m.Content).Returns("!help");
        mockMessage2.SetupGet(m => m.Author).Returns(new Mock<IUser>().Object);
        mockMessage2.SetupGet(m => m.Author.IsBot).Returns(false);

        // Act
        await registry.DispatchMessageAsync(mockMessage1.Object);
        await registry.DispatchMessageAsync(mockMessage2.Object);

        // Assert
        Assert.Equal("Handler1 Invoked", response1);
        Assert.Equal("Handler2 Invoked", response2);
    }

    [Fact]
    public async Task RegisteredEvents_BadEventDoesNotAffectOthers()
    {
        // Arrange
        var registry = new PluginEventRegistry();
        
        var event1 = "GoodEvent";
        var event2 = "BadEvent";
        var event3 = "AnotherGoodEvent";

        var response1 = "";
        var response3 = "";
        static Task<bool> predicate(IMessage _) => Task.FromResult(true);
        Task goodHandler1(IMessage _) => Task.Run(() => response1 = "Good Handler 1 Invoked");
        Task badHandler(IMessage _) => Task.Run(() => throw new Exception("Handler Error"));
        Task goodHandler3(IMessage _) => Task.Run(() => response3 = "Good Handler 3 Invoked");

        registry.SubscribeToMessage(event1, predicate, goodHandler1);
        registry.SubscribeToMessage(event2, predicate, badHandler);
        registry.SubscribeToMessage(event3, predicate, goodHandler3);

        Mock<IMessage> mockMessage = new();
        mockMessage.SetupGet(m => m.Content).Returns("Test Message");

        // Act
        await registry.DispatchMessageAsync(mockMessage.Object);

        // Assert
        Assert.Equal("Good Handler 1 Invoked", response1);
        Assert.Equal("Good Handler 3 Invoked", response3);
    }

    private static Task<bool> AdvancedPredicate(IMessage message)
    {
        if (message.IsFromBot())
        {
            return Task.FromResult(false);
        }

        if (message.IsUserMessage(out var userMessage) && userMessage is not null)
        {
            if (userMessage.Content.StartsWith('!'))
            {
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }
}