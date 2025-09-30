# Plugin Interfaces and Base Classes

## Overview

The QuantumKat Plugin SDK provides several core interfaces and base classes that define the contract and behavior for plugins. This document covers all the essential interfaces that plugins can implement.

## Core Interfaces

### IPlugin

The foundational interface that all plugins must implement.

```csharp
public interface IPlugin
{
    string Name { get; }
    string Description { get; }
    string Version { get; }
    string Author { get; }
    Dictionary<string, List<string>> PluginDependencies { get; }
    
    void Initialize(PluginBootstrapContext context);
    void RegisterServices(IServiceCollection services);
}
```

#### Properties

- **Name**: A unique identifier for the plugin
- **Description**: A brief description of what the plugin does
- **Version**: The plugin version (used for dependency resolution)
- **Author**: The plugin author's name
- **PluginDependencies**: Dependencies on other plugins with version requirements

#### Methods

- **Initialize**: Called during plugin loading to set up the plugin
- **RegisterServices**: Called to register the plugin's services with the DI container

#### Example Implementation

```csharp
public class MyPlugin : IPlugin
{
    public string Name => "MyAwesomePlugin";
    public string Description => "Does awesome things";
    public string Version => "1.0.0";
    public string Author => "John Doe";
    
    public Dictionary<string, List<string>> PluginDependencies => new()
    {
        ["SomeRequiredPlugin"] = [">=1.2.0", "<2.0.0"]
    };

    public void Initialize(PluginBootstrapContext context)
    {
        var logger = context.Logger;
        logger.LogInformation("MyAwesomePlugin initialized");
        
        // Access configuration
        var config = context.Configuration;
        var myValue = config["MyPlugin:SomeValue"];
        
        // Access other services
        var someService = context.CoreServices.GetService<ISomeService>();
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IMyPluginService, MyPluginService>();
        services.AddTransient<IMyHelper, MyHelper>();
    }
}
```

### IThreadedPlugin

An extension of `IPlugin` for plugins that need to run background tasks or services.

```csharp
public interface IThreadedPlugin : IPlugin
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
```

#### When to Use

- Plugins that need to run continuous background processes
- Plugins that host services (like web servers, schedulers, etc.)
- Plugins that need graceful startup/shutdown handling

#### Example Implementation

```csharp
public class BackgroundServicePlugin : IThreadedPlugin
{
    private readonly Timer _timer;
    private bool _isRunning;

    public string Name => "BackgroundService";
    public string Description => "Runs background tasks";
    public string Version => "1.0.0";
    public string Author => "Plugin Author";
    public Dictionary<string, List<string>> PluginDependencies => new();

    public void Initialize(PluginBootstrapContext context)
    {
        // Initialization logic
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IBackgroundTaskService, BackgroundTaskService>();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _isRunning = true;
        
        // Start your background service
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _isRunning = false;
        
        // Cleanup resources
        _timer?.Dispose();
        
        await Task.CompletedTask;
    }

    private void DoWork(object? state)
    {
        if (!_isRunning) return;
        
        // Your background work here
    }
}
```

### IPluginEventRegistry

Interface for registering event handlers within plugins.

```csharp
public interface IPluginEventRegistry
{
    void SubscribeToMessage(Func<SocketMessage, bool> predicate, Func<SocketMessage, Task> handler);
    Task DispatchMessageAsync(SocketMessage message);
}
```

#### Usage in Plugins

```csharp
public void Initialize(PluginBootstrapContext context)
{
    var eventRegistry = context.EventRegistry;
    
    // Subscribe to specific message types
    eventRegistry?.SubscribeToMessage(
        message => message.Content.StartsWith("!hello"),
        async message => {
            await message.Channel.SendMessageAsync("Hello there!");
        });
        
    // Subscribe to messages from specific channels
    eventRegistry?.SubscribeToMessage(
        message => message.Channel.Name == "bot-commands",
        HandleBotCommand);
}

private async Task HandleBotCommand(SocketMessage message)
{
    // Handle the command
}
```

### IPluginServiceProvider

Interface for managing shared services between the main application and plugins.

```csharp
public interface IPluginServiceProvider
{
    IServiceProvider ServiceProvider { get; }
    void RebuildServiceProvider();
    void RegisterServicesAndRebuild(Action<IServiceCollection> serviceRegistration);
}
```

#### Dynamic Service Registration

```csharp
public void Initialize(PluginBootstrapContext context)
{
    // Register additional services dynamically
    context.SharedServiceProvider?.RegisterServicesAndRebuild(services =>
    {
        services.AddScoped<IDynamicService, DynamicService>();
    });
}
```

## Plugin Dependency Management

### Version Requirements

The SDK supports semantic version constraints:

- `==1.2.3` - Exactly version 1.2.3
- `>=1.2.0` - Version 1.2.0 or higher
- `<=2.0.0` - Version 2.0.0 or lower
- `>1.1.0` - Higher than version 1.1.0
- `<3.0.0` - Lower than version 3.0.0
- `1.2.3` - Exactly version 1.2.3 (same as ==)

### Multiple Constraints

```csharp
public Dictionary<string, List<string>> PluginDependencies => new()
{
    ["DatabasePlugin"] = [">=1.0.0", "<2.0.0"],
    ["UtilityPlugin"] = ["==1.5.0"],
    ["LoggingPlugin"] = [">1.2.0"]
};
```

## Best Practices

### 1. Plugin Naming
- Use descriptive, unique names
- Avoid spaces and special characters
- Consider namespacing for organization

### 2. Version Management
- Follow semantic versioning (SemVer)
- Update versions when making breaking changes
- Document version compatibility

### 3. Service Registration
- Register interfaces, not concrete classes when possible
- Use appropriate service lifetimes (Singleton, Scoped, Transient)
- Avoid registering services with conflicting names

### 4. Error Handling
- Always handle exceptions in event handlers
- Log errors appropriately
- Provide meaningful error messages

### 5. Resource Management
- Implement IDisposable when managing resources
- Clean up properly in StopAsync for threaded plugins
- Avoid memory leaks in long-running plugins

## Common Patterns

### Configuration-Driven Plugin

```csharp
public class ConfigurablePlugin : IPlugin
{
    private PluginConfig _config;

    public void Initialize(PluginBootstrapContext context)
    {
        _config = context.Configuration
            .GetSection("MyPlugin")
            .Get<PluginConfig>() ?? new PluginConfig();
    }
}

public class PluginConfig
{
    public bool EnableFeature { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
    public string ConnectionString { get; set; } = "";
}
```

### Service-Dependent Plugin

```csharp
public class ServiceDependentPlugin : IPlugin
{
    private ILogger _logger;
    private IMyRequiredService _requiredService;

    public void Initialize(PluginBootstrapContext context)
    {
        _logger = context.Logger;
        _requiredService = context.CoreServices.GetRequiredService<IMyRequiredService>();
    }
}
```