using System;
using System.Reflection;
using Discord;
using Discord.WebSocket;
using Moq;
using QuantumKat.PluginSDK.Discord.Extensions;

namespace Tests;

public class ExtensionMethodTests : IDisposable
{
    public void Dispose()
    {
        // Reset the static _client field in IUserExtensions to avoid test isolation issues
        var field = typeof(IUserExtensions).GetField("_client", BindingFlags.NonPublic | BindingFlags.Static);
        field?.SetValue(null, null);
        GC.SuppressFinalize(this);
    }
    
    [Fact]
    public void ExtensionMethod_IMessage_IsUserMessage()
    {
        // Arrange
        var userMessageMock = new Mock<IUserMessage>();
        var messageMock = new Mock<IMessage>();
        
        // Act
        var isUserMessageResult1 = IMessageExtensions.IsUserMessage(userMessageMock.Object, out var userMessage);
        var isUserMessageResult2 = IMessageExtensions.IsUserMessage(messageMock.Object, out var message);

        // Assert
        Assert.True(isUserMessageResult1);
        Assert.NotNull(userMessage);
        Assert.False(isUserMessageResult2);
        Assert.Null(message);
    }

    [Fact]
    public void ExtensionMethod_IMessage_IsBot()
    {
        // Arrange
        var botMessageMock = new Mock<IMessage>();
        var notBotMessageMock = new Mock<IMessage>();
        botMessageMock.Setup(m => m.Author.IsBot).Returns(true);
        notBotMessageMock.Setup(m => m.Author.IsBot).Returns(false);

        // Act
        var isBotMessageResult = IMessageExtensions.IsFromBot(botMessageMock.Object);
        var isNotBotMessageResult = IMessageExtensions.IsFromBot(notBotMessageMock.Object);

        // Assert
        Assert.True(isBotMessageResult);
        Assert.False(isNotBotMessageResult);
    }

    [Fact]
    public void ExtensionMethod_IUser_IsClient()
    {
        // Arrange
        var discordClientMock = new Mock<IDiscordClient>();
        var currentUserMock = new Mock<ISelfUser>();
        currentUserMock.Setup(u => u.Id).Returns(1234567890UL);
        discordClientMock.Setup(c => c.CurrentUser).Returns(currentUserMock.Object);

        IUserExtensions.Initialize(discordClientMock.Object);

        var userMockClient = new Mock<IUser>();
        userMockClient.Setup(u => u.Id).Returns(1234567890UL);

        var userMockNotClient = new Mock<IUser>();
        userMockNotClient.Setup(u => u.Id).Returns(9876543210UL);

        // Act
        var isUserMockClient = IUserExtensions.IsClient(userMockClient.Object);
        var isUserMockClient2 = IUserExtensions.IsClient(userMockNotClient.Object);

        // Assert
        Assert.True(isUserMockClient);
        Assert.False(isUserMockClient2);
    }

    [Fact]
    public void ExtensionMethod_IUser_IsClientThrowsOnMissingClient()
    {
        // Arrange
        var user = new Mock<IUser>();


        // Assert
        Assert.Throws<InvalidOperationException>(() => IUserExtensions.IsClient(user.Object));
    }
}
