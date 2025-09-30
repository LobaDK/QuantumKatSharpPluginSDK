# Discord Integration and Extensions

## Overview

The QuantumKat Plugin SDK provides specialized support for Discord bot plugins through extension methods and utilities designed to work with Discord.NET. This document covers the Discord-specific features and how to build Discord bot plugins.

## Discord Extensions

### SocketMessageExtensions

The SDK provides extension methods to simplify working with Discord messages.

#### IsUserMessage

Determines if a message is from a user and provides strongly-typed access:

```csharp
public static bool IsUserMessage(this SocketMessage socketMessage, out SocketUserMessage userMessage)
```

**Usage:**
```csharp
public async Task HandleMessage(SocketMessage message)
{
    if (message.IsUserMessage(out var userMessage))
    {
        // Now you have a strongly-typed SocketUserMessage
        var hasAttachments = userMessage.Attachments.Count > 0;
        var mentionsEveryone = userMessage.MentionedEveryone;
    }
}
```

#### IsFromBot

Checks if a message is from a bot:

```csharp
public static bool IsFromBot(this SocketMessage socketMessage)
```

**Usage:**
```csharp
// Ignore bot messages
if (message.IsFromBot())
    return;

// Process user messages only
await ProcessUserMessage(message);
```

### DiscordUserExtensions

Provides utilities for working with Discord users:

```csharp
// Additional extensions can be added here as needed
```

## Building Discord Bot Plugins

### Basic Discord Plugin Structure

```csharp
public class DiscordBotPlugin : IPlugin
{
    private ILogger _logger;
    
    public string Name => "Discord Bot Plugin";
    public string Description => "Handles Discord bot commands and events";
    public string Version => "1.0.0";
    public string Author => "Bot Developer";
    public Dictionary<string, List<string>> PluginDependencies => new();

    public void Initialize(PluginBootstrapContext context)
    {
        _logger = context.Logger;
        
        // Subscribe to Discord message events
        context.EventRegistry?.SubscribeToMessage(
            ShouldHandleMessage,
            HandleDiscordMessage);
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IDiscordCommandService, DiscordCommandService>();
        services.AddTransient<IDiscordMessageHandler, DiscordMessageHandler>();
    }

    private bool ShouldHandleMessage(SocketMessage message)
    {
        // Don't handle bot messages
        if (message.IsFromBot())
            return false;
            
        // Only handle user messages
        if (!message.IsUserMessage(out var userMessage))
            return false;
            
        // Handle messages that start with command prefix
        return userMessage.Content.StartsWith("!");
    }

    private async Task HandleDiscordMessage(SocketMessage message)
    {
        if (!message.IsUserMessage(out var userMessage))
            return;

        var command = userMessage.Content.Split(' ')[0].ToLower();
        
        switch (command)
        {
            case "!hello":
                await HandleHelloCommand(userMessage);
                break;
            case "!info":
                await HandleInfoCommand(userMessage);
                break;
            default:
                await HandleUnknownCommand(userMessage);
                break;
        }
    }

    private async Task HandleHelloCommand(SocketUserMessage message)
    {
        await message.Channel.SendMessageAsync($"Hello {message.Author.Mention}! 👋");
    }

    private async Task HandleInfoCommand(SocketUserMessage message)
    {
        var embed = new EmbedBuilder()
            .WithTitle("Bot Information")
            .WithDescription("This is a Discord bot powered by QuantumKat Plugin SDK")
            .WithColor(Color.Blue)
            .AddField("Plugin", Name, true)
            .AddField("Version", Version, true)
            .WithFooter($"Requested by {message.Author.Username}")
            .WithCurrentTimestamp()
            .Build();

        await message.Channel.SendMessageAsync(embed: embed);
    }

    private async Task HandleUnknownCommand(SocketUserMessage message)
    {
        var availableCommands = new[] { "!hello", "!info" };
        var commandList = string.Join(", ", availableCommands);
        
        await message.Channel.SendMessageAsync(
            $"Unknown command. Available commands: {commandList}");
    }
}
```

### Advanced Discord Plugin with Command Framework

```csharp
public class AdvancedDiscordPlugin : IThreadedPlugin
{
    private ILogger _logger;
    private DiscordSocketClient _discordClient;
    private CommandService _commandService;
    private IServiceProvider _serviceProvider;
    private DiscordPluginSettings _settings;
    
    public string Name => "Advanced Discord Plugin";
    public string Description => "Full-featured Discord bot with command framework";
    public string Version => "2.0.0";
    public string Author => "Advanced Bot Developer";
    public Dictionary<string, List<string>> PluginDependencies => new();

    public void Initialize(PluginBootstrapContext context)
    {
        _logger = context.Logger;
        _serviceProvider = context.CoreServices;
        
        // Load plugin-specific settings
        _settings = context.Configuration
            .GetSection("AdvancedDiscordPlugin")
            .Get<DiscordPluginSettings>() ?? new DiscordPluginSettings();
            
        // Initialize Discord client
        _discordClient = new DiscordSocketClient(new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Info,
            MessageCacheSize = 100
        });
        
        // Initialize command service
        _commandService = new CommandService(new CommandServiceConfig
        {
            LogLevel = LogSeverity.Info,
            CaseSensitiveCommands = false
        });
        
        // Setup event handlers
        _discordClient.Log += LogAsync;
        _discordClient.MessageReceived += MessageReceivedAsync;
        _commandService.Log += LogAsync;
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton(_discordClient);
        services.AddSingleton(_commandService);
        services.AddSingleton<IDiscordService, DiscordService>();
        services.AddTransient<UserModule>();
        services.AddTransient<AdminModule>();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Discord plugin...");
        
        // Login and start
        await _discordClient.LoginAsync(TokenType.Bot, _settings.BotToken);
        await _discordClient.StartAsync();
        
        // Load command modules
        await _commandService.AddModuleAsync<UserModule>(_serviceProvider);
        await _commandService.AddModuleAsync<AdminModule>(_serviceProvider);
        
        _logger.LogInformation("Discord plugin started successfully");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Discord plugin...");
        
        await _discordClient.StopAsync();
        await _discordClient.LogoutAsync();
        _discordClient.Dispose();
        
        _logger.LogInformation("Discord plugin stopped");
    }

    private Task LogAsync(LogMessage log)
    {
        var logLevel = log.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information
        };
        
        _logger.Log(logLevel, log.Exception, "[Discord] {Message}", log.Message);
        return Task.CompletedTask;
    }

    private async Task MessageReceivedAsync(SocketMessage message)
    {
        // Don't handle bot messages
        if (message.IsFromBot())
            return;
            
        if (!message.IsUserMessage(out var userMessage))
            return;

        // Check for command prefix
        var argPos = 0;
        if (!(userMessage.HasStringPrefix(_settings.CommandPrefix, ref argPos) ||
              userMessage.HasMentionPrefix(_discordClient.CurrentUser, ref argPos)))
            return;

        var context = new SocketCommandContext(_discordClient, userMessage);
        
        var result = await _commandService.ExecuteAsync(
            context: context,
            argPos: argPos,
            services: _serviceProvider);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Command failed: {Error}", result.ErrorReason);
            
            if (result.Error == CommandError.UnknownCommand)
            {
                await context.Channel.SendMessageAsync("Unknown command. Use `!help` for available commands.");
            }
            else
            {
                await context.Channel.SendMessageAsync($"Error: {result.ErrorReason}");
            }
        }
    }
}

// Plugin settings
public class DiscordPluginSettings
{
    public string BotToken { get; set; } = "";
    public string CommandPrefix { get; set; } = "!";
    public List<ulong> AdminUsers { get; set; } = new();
    public ulong LogChannelId { get; set; } = 0;
}

// Command modules
[Group("user")]
public class UserModule : ModuleBase<SocketCommandContext>
{
    private readonly IDiscordService _discordService;
    
    public UserModule(IDiscordService discordService)
    {
        _discordService = discordService;
    }
    
    [Command("info")]
    [Summary("Get information about a user")]
    public async Task UserInfo(SocketGuildUser user = null)
    {
        user ??= Context.User as SocketGuildUser;
        
        var embed = new EmbedBuilder()
            .WithTitle($"User Information: {user.DisplayName}")
            .WithThumbnailUrl(user.GetAvatarUrl())
            .AddField("Username", user.Username, true)
            .AddField("Discriminator", user.Discriminator, true)
            .AddField("ID", user.Id, true)
            .AddField("Created", user.CreatedAt.ToString("yyyy-MM-dd"), true)
            .AddField("Joined", user.JoinedAt?.ToString("yyyy-MM-dd") ?? "Unknown", true)
            .WithColor(Color.Green)
            .Build();
            
        await ReplyAsync(embed: embed);
    }
    
    [Command("avatar")]
    [Summary("Get a user's avatar")]
    public async Task Avatar(SocketUser user = null)
    {
        user ??= Context.User;
        
        var embed = new EmbedBuilder()
            .WithTitle($"{user.Username}'s Avatar")
            .WithImageUrl(user.GetAvatarUrl(size: 512))
            .WithColor(Color.Blue)
            .Build();
            
        await ReplyAsync(embed: embed);
    }
}

[Group("admin")]
[RequireUserPermission(GuildPermission.Administrator)]
public class AdminModule : ModuleBase<SocketCommandContext>
{
    [Command("purge")]
    [Summary("Delete multiple messages")]
    public async Task Purge(int count = 10)
    {
        if (count > 100)
        {
            await ReplyAsync("Cannot delete more than 100 messages at once.");
            return;
        }
        
        var messages = await Context.Channel.GetMessagesAsync(count + 1).FlattenAsync();
        var messagesToDelete = messages.Where(m => 
            (DateTimeOffset.UtcNow - m.Timestamp).TotalDays < 14);
            
        if (Context.Channel is SocketTextChannel textChannel)
        {
            await textChannel.DeleteMessagesAsync(messagesToDelete);
            
            var confirmMessage = await ReplyAsync($"Deleted {messagesToDelete.Count()} messages.");
            
            // Delete confirmation message after 5 seconds
            _ = Task.Delay(5000).ContinueWith(async _ => 
            {
                try { await confirmMessage.DeleteAsync(); } 
                catch { /* Ignore if already deleted */ }
            });
        }
    }
}
```

## Event-Driven Discord Plugins

### Message Event Handling

```csharp
public class MessageEventPlugin : IPlugin
{
    public void Initialize(PluginBootstrapContext context)
    {
        var eventRegistry = context.EventRegistry;
        
        // Handle welcome messages
        eventRegistry?.SubscribeToMessage(
            message => IsWelcomeChannel(message) && IsNewMember(message),
            SendWelcomeMessage);
            
        // Handle reaction roles
        eventRegistry?.SubscribeToMessage(
            message => message.Content.Contains("role"),
            HandleRoleRequest);
            
        // Moderate inappropriate content
        eventRegistry?.SubscribeToMessage(
            message => ContainsInappropriateContent(message.Content),
            ModerateMessage);
    }
    
    private bool IsWelcomeChannel(SocketMessage message)
    {
        return message.Channel.Name.Contains("welcome") || 
               message.Channel.Name.Contains("general");
    }
    
    private bool IsNewMember(SocketMessage message)
    {
        if (message.Author is SocketGuildUser guildUser)
        {
            var membershipAge = DateTime.UtcNow - guildUser.JoinedAt;
            return membershipAge?.TotalHours < 24;
        }
        return false;
    }
    
    private async Task SendWelcomeMessage(SocketMessage message)
    {
        var welcomeEmbed = new EmbedBuilder()
            .WithTitle("Welcome!")
            .WithDescription($"Welcome to the server, {message.Author.Mention}!")
            .WithColor(Color.Green)
            .WithThumbnailUrl(message.Author.GetAvatarUrl())
            .Build();
            
        await message.Channel.SendMessageAsync(embed: welcomeEmbed);
    }
}
```

## Utility Services

### Discord Service Interface

```csharp
public interface IDiscordService
{
    Task<bool> IsUserAdminAsync(ulong userId, ulong guildId);
    Task<SocketGuildUser> GetGuildUserAsync(ulong userId, ulong guildId);
    Task LogToChannelAsync(ulong channelId, string message);
    Task<bool> HasPermissionAsync(SocketGuildUser user, GuildPermission permission);
}

public class DiscordService : IDiscordService
{
    private readonly DiscordSocketClient _client;
    private readonly ILogger<DiscordService> _logger;
    
    public DiscordService(DiscordSocketClient client, ILogger<DiscordService> logger)
    {
        _client = client;
        _logger = logger;
    }
    
    public async Task<bool> IsUserAdminAsync(ulong userId, ulong guildId)
    {
        var guild = _client.GetGuild(guildId);
        var user = guild?.GetUser(userId);
        
        return user?.GuildPermissions.Administrator ?? false;
    }
    
    public async Task<SocketGuildUser> GetGuildUserAsync(ulong userId, ulong guildId)
    {
        var guild = _client.GetGuild(guildId);
        return guild?.GetUser(userId);
    }
    
    public async Task LogToChannelAsync(ulong channelId, string message)
    {
        var channel = _client.GetChannel(channelId) as IMessageChannel;
        if (channel != null)
        {
            await channel.SendMessageAsync(message);
        }
    }
    
    public async Task<bool> HasPermissionAsync(SocketGuildUser user, GuildPermission permission)
    {
        return user.GuildPermissions.Has(permission);
    }
}
```

## Best Practices for Discord Plugins

### 1. Error Handling
- Always handle Discord API rate limits
- Implement retry logic for failed requests
- Log Discord-related errors appropriately

### 2. Performance
- Use message caching judiciously
- Implement command cooldowns
- Avoid unnecessary API calls

### 3. Security
- Validate user permissions
- Sanitize user input
- Implement rate limiting for commands

### 4. User Experience
- Provide helpful error messages
- Use embeds for rich content
- Implement command help and documentation

### 5. Resource Management
- Properly dispose of Discord client
- Handle connection failures gracefully
- Implement reconnection logic

## Common Discord Plugin Patterns

### Command Cooldowns

```csharp
public class CooldownService
{
    private readonly Dictionary<(ulong userId, string command), DateTime> _cooldowns = new();
    
    public bool IsOnCooldown(ulong userId, string command, TimeSpan cooldownPeriod)
    {
        var key = (userId, command);
        if (_cooldowns.TryGetValue(key, out var lastUsed))
        {
            return DateTime.UtcNow - lastUsed < cooldownPeriod;
        }
        return false;
    }
    
    public void SetCooldown(ulong userId, string command)
    {
        _cooldowns[(userId, command)] = DateTime.UtcNow;
    }
}
```

### Permission Checks

```csharp
public class RequireCustomPermissionAttribute : PreconditionAttribute
{
    private readonly string _permission;
    
    public RequireCustomPermissionAttribute(string permission)
    {
        _permission = permission;
    }
    
    public override async Task<PreconditionResult> CheckPermissionsAsync(
        ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        var permissionService = services.GetRequiredService<IPermissionService>();
        var hasPermission = await permissionService.HasPermissionAsync(
            context.User.Id, _permission);
            
        return hasPermission 
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError($"You need the '{_permission}' permission to use this command.");
    }
}
```