# API Reference

## Overview

This document provides a comprehensive API reference for the QuantumKat Plugin SDK. It covers all public interfaces, classes, and methods available for plugin development.

## Core Namespace: QuantumKat.PluginSDK.Core

### Interfaces

#### IPlugin
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

**Properties:**
- `Name`: Unique plugin identifier
- `Description`: Brief plugin description
- `Version`: Plugin version (semantic versioning recommended)
- `Author`: Plugin author name
- `PluginDependencies`: Dictionary of plugin dependencies with version constraints

**Methods:**
- `Initialize(PluginBootstrapContext)`: Called during plugin initialization
- `RegisterServices(IServiceCollection)`: Register plugin services with DI container

#### IThreadedPlugin
```csharp
public interface IThreadedPlugin : IPlugin
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
```

**Methods:**
- `StartAsync(CancellationToken)`: Start plugin background operations
- `StopAsync(CancellationToken)`: Stop plugin background operations gracefully

#### IPluginEventRegistry
```csharp
public interface IPluginEventRegistry
{
    void SubscribeToMessage(Func<SocketMessage, bool> predicate, Func<SocketMessage, Task> handler);
    Task DispatchMessageAsync(SocketMessage message);
}
```

**Methods:**
- `SubscribeToMessage`: Register message event handler with predicate filter
- `DispatchMessageAsync`: Dispatch message to all registered handlers

#### IPluginServiceProvider
```csharp
public interface IPluginServiceProvider
{
    IServiceProvider ServiceProvider { get; }
    void RebuildServiceProvider();
    void RegisterServicesAndRebuild(Action<IServiceCollection> serviceRegistration);
}
```

**Properties:**
- `ServiceProvider`: Current shared service provider instance

**Methods:**
- `RebuildServiceProvider()`: Rebuild service provider with current services
- `RegisterServicesAndRebuild(Action<IServiceCollection>)`: Register services and rebuild

### Classes

#### PluginManager
```csharp
public class PluginManager
{
    public PluginManager(IPluginServiceProvider sharedServiceProvider, IConfiguration configuration, ILogger logger);
    
    public void LoadPlugins(IEnumerable<string> pluginPaths);
    public void RegisterAllPluginServices();
    public async Task StartAllPluginsAsync(CancellationToken cancellationToken);
    public async Task StopAllPluginsAsync(CancellationToken cancellationToken);
    public async Task StartPluginAsync(IThreadedPlugin plugin, CancellationToken cancellationToken);
    public async Task StopPluginAsync(IThreadedPlugin plugin, CancellationToken cancellationToken);
    public async Task DispatchMessageAsync(SocketMessage message);
}
```

**Constructor Parameters:**
- `sharedServiceProvider`: Shared service provider for DI management
- `configuration`: Application configuration
- `logger`: Logger instance

**Methods:**
- `LoadPlugins(IEnumerable<string>)`: Load plugins from assembly paths
- `RegisterAllPluginServices()`: Register all plugin services
- `StartAllPluginsAsync(CancellationToken)`: Start all threaded plugins
- `StopAllPluginsAsync(CancellationToken)`: Stop all threaded plugins
- `StartPluginAsync(IThreadedPlugin, CancellationToken)`: Start specific plugin
- `StopPluginAsync(IThreadedPlugin, CancellationToken)`: Stop specific plugin
- `DispatchMessageAsync(SocketMessage)`: Dispatch message to plugins

#### PluginBootstrapContext
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

**Properties:**
- `Configuration`: Application configuration
- `Logger`: Logger instance for plugin use
- `CoreServices`: Core application services
- `PluginDirectory`: Directory containing the plugin
- `EventRegistry`: Event registry for message handling (optional)
- `SharedServiceProvider`: Shared service provider for dynamic registration (optional)

#### SharedServiceProvider
```csharp
public class SharedServiceProvider : IPluginServiceProvider, IDisposable
{
    public SharedServiceProvider(IServiceCollection serviceCollection);
    
    public IServiceProvider ServiceProvider { get; }
    public void RebuildServiceProvider();
    public void RegisterServicesAndRebuild(Action<IServiceCollection> serviceRegistration);
    public void Dispose();
}
```

**Constructor Parameters:**
- `serviceCollection`: Service collection to build provider from

#### PluginEventRegistry
```csharp
public class PluginEventRegistry : IPluginEventRegistry
{
    public void SubscribeToMessage(Func<SocketMessage, bool> predicate, Func<SocketMessage, Task> handler);
    public async Task DispatchMessageAsync(SocketMessage message);
}
```

#### PluginLoadContext
```csharp
public class PluginLoadContext : AssemblyLoadContext
{
    public PluginLoadContext(string pluginPath);
    protected override Assembly Load(AssemblyName assemblyName);
}
```

**Constructor Parameters:**
- `pluginPath`: Path to the plugin assembly

## Extensions Namespace: QuantumKat.PluginSDK.Core.Extensions

### ServiceCollectionExtensions
```csharp
public static class ServiceCollectionExtensions
{
    public static PluginSDKBuilder AddPluginSDK(this IServiceCollection services, IConfiguration configuration, ILogger logger);
}
```

**Extension Methods:**
- `AddPluginSDK(IServiceCollection, IConfiguration, ILogger)`: Add Plugin SDK services

### PluginSDKBuilder
```csharp
public class PluginSDKBuilder
{
    public IPluginServiceProvider SharedServiceProvider { get; }
    
    public PluginSDKBuilder LoadPlugins(IEnumerable<string> pluginPaths);
    public PluginSDKBuilder ConfigureServices(Action<IServiceCollection> configureServices);
    public IServiceProvider Build();
}
```

**Properties:**
- `SharedServiceProvider`: Access to shared service provider

**Methods:**
- `LoadPlugins(IEnumerable<string>)`: Load plugins and register services
- `ConfigureServices(Action<IServiceCollection>)`: Configure additional services
- `Build()`: Build final service provider

## Settings Namespace: QuantumKat.PluginSDK.Settings

### ISetting
```csharp
public interface ISetting
{
    // Marker interface for settings classes
}
```

### SettingsManager
```csharp
public class SettingsManager
{
    public SettingsManager(string file);
    
    public IConfiguration GetConfiguration<T>() where T : new();
    public void Save(object settings);
    public static T InitializeSettings<T>() where T : new();
}
```

**Constructor Parameters:**
- `file`: Settings file name/path

**Methods:**
- `GetConfiguration<T>()`: Get configuration with automatic file creation
- `Save(object)`: Save settings to file
- `InitializeSettings<T>()`: Initialize settings with default values

## Attributes Namespace: QuantumKat.PluginSDK.Attributes

### SettingCallbackAttribute
```csharp
[AttributeUsage(AttributeTargets.Method)]
public class SettingCallbackAttribute : Attribute
{
    // Marker attribute for callback methods
}
```

**Usage:**
Applied to methods that should be called when settings are initialized.

## Discord Extensions Namespace: QuantumKat.PluginSDK.Discord.Extensions

### SocketMessageExtensions
```csharp
public static class SocketMessageExtensions
{
    public static bool IsUserMessage(this SocketMessage socketMessage, out SocketUserMessage userMessage);
    public static bool IsFromBot(this SocketMessage socketMessage);
}
```

**Extension Methods:**
- `IsUserMessage(SocketMessage, out SocketUserMessage)`: Check if message is from user
- `IsFromBot(SocketMessage)`: Check if message is from bot

### DiscordUserExtensions
```csharp
public static class DiscordUserExtensions
{
    // Extension methods for Discord users
}
```

## Version Constraints

The SDK supports the following version constraint formats:

- `==x.y.z` - Exactly version x.y.z
- `>=x.y.z` - Version x.y.z or higher
- `<=x.y.z` - Version x.y.z or lower
- `>x.y.z` - Higher than version x.y.z
- `<x.y.z` - Lower than version x.y.z
- `x.y.z` - Exactly version x.y.z (same as ==)

Multiple constraints can be specified in a list:
```csharp
[">=1.0.0", "<2.0.0"] // Version 1.0.0 or higher, but less than 2.0.0
```

## Error Handling

### Common Exceptions

#### PluginLoadException
Thrown when a plugin fails to load properly.

#### DependencyResolutionException
Thrown when plugin dependencies cannot be resolved.

#### CircularDependencyException
Thrown when circular dependencies are detected between plugins.

### Exception Handling Best Practices

1. **Catch specific exceptions** where possible
2. **Log errors appropriately** using the provided logger
3. **Don't let plugin errors crash the application**
4. **Provide meaningful error messages** to users

## Lifecycle Events

### Plugin Lifecycle
1. **Discovery** - Plugin assemblies are scanned
2. **Dependency Resolution** - Dependencies are analyzed and ordered
3. **Loading** - Plugins are loaded into isolated contexts
4. **Instantiation** - Plugin instances are created
5. **Initialization** - `Initialize()` method is called
6. **Service Registration** - `RegisterServices()` method is called
7. **Starting** - `StartAsync()` method is called (for threaded plugins)
8. **Runtime** - Plugin operates normally
9. **Stopping** - `StopAsync()` method is called (for threaded plugins)
10. **Disposal** - Resources are cleaned up

### Service Provider Lifecycle
1. **Initial Build** - Service provider created from main application services
2. **Plugin Registration** - Plugins register their services
3. **Rebuild** - Service provider rebuilt with all services
4. **Runtime** - Shared service provider used throughout application
5. **Disposal** - Service provider disposed during shutdown

## Thread Safety

### Thread-Safe Components
- `PluginEventRegistry` - Thread-safe for concurrent message dispatch
- `SharedServiceProvider` - Thread-safe for service provider access
- `PluginManager` - Thread-safe for plugin management operations

### Non-Thread-Safe Components
- Service registration should be done during initialization only
- Plugin loading should be done on a single thread
- Settings modification should be synchronized

## Performance Considerations

### Memory Management
- Plugin assemblies are loaded in collectible contexts
- Service providers are properly disposed
- Event handlers maintain weak references where appropriate

### Optimization Tips
- Use lazy loading for expensive services
- Implement caching for frequently accessed data
- Use object pooling for frequently allocated objects
- Minimize allocations in hot paths

## Debugging and Diagnostics

### Logging Categories
- `QuantumKat.PluginSDK.Core.PluginManager` - Plugin management operations
- `QuantumKat.PluginSDK.Core.PluginEventRegistry` - Event dispatching
- `QuantumKat.PluginSDK.Settings.SettingsManager` - Settings operations

### Diagnostic Information
- Plugin load times and success/failure status
- Dependency resolution results
- Service registration statistics
- Event handler registration and execution times