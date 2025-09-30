# Plugin SDK Dependency Injection Integration

## Overview

The Plugin SDK now supports shared dependency injection between the main application and plugins. This ensures that both the main application and plugins share the same service provider instance, maintaining consistency and allowing services to be shared seamlessly.

## Key Components

### 1. IPluginServiceProvider
An interface that manages a shared service provider that can be rebuilt as plugins register new services.

### 2. SharedServiceProvider
The concrete implementation of `IPluginServiceProvider` that maintains a single service collection and rebuilds the service provider when needed.

### 3. Updated PluginBootstrapContext
Now includes a `SharedServiceProvider` property that plugins can use to register additional services dynamically.

## Usage Examples

### Basic Setup

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuantumKat.PluginSDK.Core.Extensions;

// Create your service collection
var services = new ServiceCollection();

// Add your main application services
services.AddLogging();
services.AddSingleton<IMyMainService, MyMainService>();

// Add configuration
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var logger = LoggerFactory.Create(builder => builder.AddConsole())
    .CreateLogger<Program>();

// Add Plugin SDK and load plugins
var serviceProvider = services
    .AddPluginSDK(configuration, logger)
    .LoadPlugins(["path/to/plugin1.dll", "path/to/plugin2.dll"])
    .Build();

// Now both your main application and plugins share the same service provider
var myService = serviceProvider.GetRequiredService<IMyMainService>();
var pluginService = serviceProvider.GetService<IPluginProvidedService>();
```

### Advanced Configuration

```csharp
var serviceProvider = services
    .AddPluginSDK(configuration, logger)
    .ConfigureServices(s => {
        // Add additional services before loading plugins
        s.AddTransient<IAdditionalService, AdditionalService>();
    })
    .LoadPlugins(pluginPaths)
    .ConfigureServices(s => {
        // Add services after plugins are loaded
        s.AddSingleton<IPostPluginService, PostPluginService>();
    })
    .Build();
```

### Plugin Implementation Example

```csharp
public class MyPlugin : IPlugin
{
    public string Name => "My Plugin";
    public string Description => "Example plugin";
    public string Version => "1.0.0";
    public string Author => "Plugin Author";
    public Dictionary<string, List<string>> PluginDependencies => new();

    private PluginBootstrapContext? _context;

    public void Initialize(PluginBootstrapContext context)
    {
        _context = context;
        
        // Access shared services
        var mainService = context.CoreServices.GetRequiredService<IMyMainService>();
        
        // Optionally register additional services dynamically
        context.SharedServiceProvider?.RegisterServicesAndRebuild(services => {
            services.AddTransient<IDynamicPluginService, DynamicPluginService>();
        });
    }

    public void RegisterServices(IServiceCollection services)
    {
        // Register plugin services
        services.AddSingleton<IPluginService, PluginService>();
        services.AddTransient<IPluginHelper, PluginHelper>();
    }
}
```

## Benefits

1. **Shared DI Container**: Both main application and plugins share the same service provider instance
2. **Service Consistency**: Services registered by plugins are available to the main application and vice versa
3. **Dynamic Registration**: Plugins can register additional services during initialization
4. **Clean Architecture**: Clear separation of concerns with proper DI integration
5. **Backwards Compatibility**: Existing plugins continue to work with minimal changes

## Migration Guide

### For Main Applications

**Before:**
```csharp
var services = new ServiceCollection();
// ... register services ...
var serviceProvider = services.BuildServiceProvider();

var pluginManager = new PluginManager(services, configuration, logger, serviceProvider);
pluginManager.LoadPlugins(pluginPaths);
pluginManager.RegisterAllPluginServices();
```

**After:**
```csharp
var services = new ServiceCollection();
// ... register services ...

var serviceProvider = services
    .AddPluginSDK(configuration, logger)
    .LoadPlugins(pluginPaths)
    .Build();
```

### For Plugins

The plugin interface remains the same, but plugins now have access to the `SharedServiceProvider` through the bootstrap context for dynamic service registration.

## Thread Safety

The `SharedServiceProvider` is designed to be thread-safe for service provider access, but service registration should be done during plugin initialization to avoid race conditions.