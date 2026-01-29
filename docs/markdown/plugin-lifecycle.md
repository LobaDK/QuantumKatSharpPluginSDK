# Plugin Lifecycle and Management

## Overview

The QuantumKat Plugin SDK provides a comprehensive plugin lifecycle management system through the `PluginManager` class. This document covers how plugins are loaded, initialized, managed, and disposed of throughout their lifecycle.

## Plugin Lifecycle Phases

### 1. Discovery Phase
The plugin manager scans specified directories for plugin assemblies and identifies types that implement `IPlugin`.

### 2. Dependency Resolution Phase
Dependencies between plugins are analyzed and resolved, ensuring plugins are loaded in the correct order.

### 3. Loading Phase
Plugin assemblies are loaded into isolated contexts to prevent conflicts.

### 4. Initialization Phase
Plugins are instantiated and their `Initialize` method is called.

### 5. Service Registration Phase
Plugins register their services with the dependency injection container.

### 6. Runtime Phase
Plugins operate normally, handling events and providing services.

### 7. Shutdown Phase
Threaded plugins are gracefully stopped and resources are cleaned up.

## PluginManager

The `PluginManager` class orchestrates the entire plugin lifecycle.

### Constructor

```csharp
public PluginManager(
    IPluginServiceProvider sharedServiceProvider, 
    IConfiguration configuration, 
    ILogger logger)
```

### Key Methods

#### LoadPlugins
```csharp
public void LoadPlugins(IEnumerable<string> pluginPaths, bool throwOnError = true)
```

Loads plugins from the specified assembly paths with dependency resolution.

**Process:**
1. Scans each assembly for `IPlugin` implementations
2. Extracts metadata (name, version, dependencies)
3. Resolves dependency order using topological sorting
4. Detects and reports circular dependencies
5. Loads plugins in dependency order
6. Initializes each plugin with a `PluginBootstrapContext`

**Parameters:**
- `pluginPaths`: Collection of paths to plugin assemblies
- `throwOnError`: If true, throws exceptions on plugin loading failures; if false, logs warnings and continues

#### RegisterAllPluginServices
```csharp
public void RegisterAllPluginServices()
```

Registers services from all loaded plugins into the shared service provider.

#### Threaded Plugin Management
```csharp
public async Task StartAllPluginsAsync(CancellationToken cancellationToken)
public async Task StopAllPluginsAsync(CancellationToken cancellationToken)
public async Task StartPluginAsync(IThreadedPlugin plugin, CancellationToken cancellationToken)
public async Task StopPluginAsync(IThreadedPlugin plugin, CancellationToken cancellationToken)
```

## Plugin Load Context

Each plugin is loaded in its own `PluginLoadContext` for isolation:

```csharp
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }
}
```

### Benefits of Isolation
- **Prevents Version Conflicts**: Different plugins can use different versions of the same dependency
- **Memory Management**: Plugins can be unloaded to free memory
- **Security**: Plugins operate in isolated contexts
- **Stability**: Plugin crashes don't affect other plugins

## PluginBootstrapContext

The bootstrap context provides plugins with essential services and configuration:

```csharp
public class PluginBootstrapContext
{
    public required IConfiguration Configuration { get; init; }
    public required ILogger Logger { get; init; }
    public required IServiceProvider CoreServices { get; init; }
    public required string PluginDirectory { get; init; }
    public IPluginEventRegistry? EventRegistry { get; init; }
    public IPluginServiceProvider? SharedServiceProvider { get; init; }
}
```

### Usage in Plugin Initialization

```csharp
public void Initialize(PluginBootstrapContext context)
{
    // Access configuration
    var myConfig = context.Configuration.GetSection("MyPlugin");
    
    // Use logger
    context.Logger.LogInformation("Plugin initializing...");
    
    // Access core services
    var dbContext = context.CoreServices.GetRequiredService<IDbContext>();
    
    // Register for named events with async predicate
    context.EventRegistry?.SubscribeToMessage(
        "command-handler",
        async msg => msg.Content.StartsWith("!"),
        HandleCommand);
    
    // Register additional services dynamically
    context.SharedServiceProvider?.RegisterServicesAndRebuild(services =>
    {
        services.AddTransient<IMyDynamicService, MyDynamicService>();
    });
}
```

## Dependency Resolution

### Dependency Declaration

Plugins declare dependencies using the `PluginDependencies` property:

```csharp
public Dictionary<string, List<string>> PluginDependencies => new()
{
    ["CorePlugin"] = [">=1.0.0"],
    ["DatabasePlugin"] = [">=2.1.0", "<3.0.0"],
    ["UtilityPlugin"] = ["==1.5.0"]
};
```

### Resolution Algorithm

The plugin manager uses topological sorting to resolve dependencies:

1. **Dependency Graph Construction**: Build a graph of plugin dependencies
2. **Cycle Detection**: Detect and report circular dependencies
3. **Version Validation**: Verify that dependency versions satisfy constraints
4. **Topological Sort**: Order plugins so dependencies load before dependents

### Error Handling

Common dependency resolution errors:

- **Missing Dependency**: Required plugin not found
- **Version Mismatch**: Available version doesn't satisfy constraints
- **Circular Dependency**: Plugins have circular references

## Event System

The plugin SDK includes an event system for inter-plugin communication using named subscriptions.

### PluginEventRegistry

```csharp
public class PluginEventRegistry : IPluginEventRegistry
{
    public void SubscribeToMessage(string name, Func<IMessage, Task<bool>> predicate, Func<IMessage, Task> handler);
    public void UnsubscribeFromMessage(string name);
    public void ClearAllSubscriptions();
    public bool IsSubscribed(string name);
    public Dictionary<string, (Func<IMessage, Task<bool>> predicate, Func<IMessage, Task> handler)> GetSubscriptions();
    public async Task DispatchMessageAsync(IMessage message);
}
```

### Event Registration with Named Subscriptions

```csharp
// In plugin initialization
context.EventRegistry?.SubscribeToMessage(
    "hello-command",
    async message => message.Content.StartsWith("!hello"),
    async message => {
        await message.Channel.SendMessageAsync($"Hello {message.Author.Username}!");
    });
```

### Event Dispatching

```csharp
// In your application - dispatch to all registered handlers
await pluginManager.DispatchMessageAsync(discordMessage);
```

### Subscription Management

```csharp
// Check if a handler is registered
bool hasHandler = eventRegistry?.IsSubscribed("hello-command") ?? false;

// Get all subscriptions from this plugin
var subs = eventRegistry?.GetSubscriptions();

// Unregister a specific handler
eventRegistry?.UnsubscribeFromMessage("hello-command");

// Clear all handlers from this plugin
eventRegistry?.ClearAllSubscriptions();
```

## Complete Lifecycle Example

Here's a complete example showing plugin lifecycle management:

### Application Setup

```csharp
using QuantumKat.PluginSDK.Core.Extensions;

var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton<IMyMainService, MyMainService>();

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var logger = LoggerFactory.Create(builder => builder.AddConsole())
    .CreateLogger<Program>();

// Setup plugin SDK
var pluginBuilder = services.AddPluginSDK(configuration, logger);

// Load plugins
var pluginPaths = Directory.GetFiles("plugins", "*.dll");
pluginBuilder.LoadPlugins(pluginPaths);

// Build service provider
var serviceProvider = pluginBuilder.Build();

// Get plugin manager
var pluginManager = serviceProvider.GetRequiredService<PluginManager>();

// Start threaded plugins
var cancellationTokenSource = new CancellationTokenSource();
await pluginManager.StartAllPluginsAsync(cancellationTokenSource.Token);

// Application runtime...

// Graceful shutdown
await pluginManager.StopAllPluginsAsync(cancellationTokenSource.Token);
```

### Plugin Implementation

```csharp
public class ExamplePlugin : IThreadedPlugin
{
    private Timer? _timer;
    private ILogger? _logger;
    private bool _isRunning;

    public string Name => "ExamplePlugin";
    public string Description => "Example threaded plugin";
    public string Version => "1.0.0";
    public string Author => "Plugin Developer";
    
    public Dictionary<string, List<string>> PluginDependencies => new()
    {
        ["CoreUtilityPlugin"] = [">=1.0.0"]
    };

    public void Initialize(PluginBootstrapContext context)
    {
        _logger = context.Logger;
        _logger.LogInformation("ExamplePlugin initializing...");
        
        // Subscribe to events with named subscriptions
        context.EventRegistry?.SubscribeToMessage(
            "status-command",
            async msg => msg.Content == "!status",
            HandleStatusCommand);
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IExampleService, ExampleService>();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("ExamplePlugin starting...");
        _isRunning = true;
        
        // Start background timer
        _timer = new Timer(DoPeriodicWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("ExamplePlugin stopping...");
        _isRunning = false;
        
        _timer?.Dispose();
        _timer = null;
        
        await Task.CompletedTask;
    }

    private async Task HandleStatusCommand(IMessage message)
    {
        var status = _isRunning ? "Running" : "Stopped";
        await message.Channel.SendMessageAsync($"ExamplePlugin status: {status}");
    }

    private void DoPeriodicWork(object? state)
    {
        if (!_isRunning) return;
        
        _logger?.LogDebug("ExamplePlugin doing periodic work...");
        // Perform background tasks
    }
}
```

## Best Practices

### 1. Plugin Organization
- Keep plugins focused on single responsibilities
- Use clear, descriptive names
- Organize related plugins in groups

### 2. Dependency Management
- Minimize dependencies when possible
- Use version ranges rather than exact versions
- Document dependency requirements

### 3. Resource Management
- Always implement proper cleanup in `StopAsync`
- Dispose of resources in threaded plugins
- Handle cancellation tokens properly

### 4. Error Handling
- Log errors appropriately
- Don't let plugin errors crash the application
- Provide meaningful error messages

### 5. Performance
- Avoid blocking operations in initialization
- Use async/await properly in threaded plugins
- Monitor memory usage in long-running plugins

### 6. Testing
- Test plugins in isolation
- Verify dependency resolution
- Test startup and shutdown procedures