# Settings and Configuration Management

## Overview

The QuantumKat Plugin SDK provides a robust settings management system through the `SettingsManager` class and supporting attributes. This system allows plugins to define, load, save, and manage configuration settings with automatic serialization and callback support.

## SettingsManager

The `SettingsManager` class handles JSON-based configuration with automatic initialization and file management.

### Constructor

```csharp
public SettingsManager(string file)
```

### Key Features

- **Automatic File Creation**: Creates settings files if they don't exist
- **JSON Serialization**: Uses System.Text.Json with pretty printing
- **Configuration Integration**: Integrates with Microsoft.Extensions.Configuration
- **Environment Variable Support**: Supports environment variable overrides
- **Command Line Support**: Supports command line argument overrides

### Basic Usage

```csharp
// Create settings manager
var settingsManager = new SettingsManager("plugin-settings.json");

// Get configuration
var configuration = settingsManager.GetConfiguration<MyPluginSettings>();

// Access settings
var myValue = configuration["SomeProperty"];
var connectionString = configuration["Database:ConnectionString"];
```

## Settings Classes

Define your settings using POCOs with proper attributes:

### Basic Settings Class

```csharp
public class MyPluginSettings
{
    public string ApplicationName { get; set; } = "My Plugin";
    public int MaxRetries { get; set; } = 3;
    public bool EnableLogging { get; set; } = true;
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
}
```

### Complex Settings with Nested Objects

```csharp
public class DatabaseSettings
{
    public string ConnectionString { get; set; } = "";
    public int CommandTimeout { get; set; } = 30;
    public bool EnableRetryOnFailure { get; set; } = true;
}

public class ApiSettings
{
    public string BaseUrl { get; set; } = "https://api.example.com";
    public string ApiKey { get; set; } = "";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}

public class MyPluginSettings
{
    public string Name { get; set; } = "My Plugin";
    public DatabaseSettings Database { get; set; } = new();
    public ApiSettings Api { get; set; } = new();
    public List<string> EnabledFeatures { get; set; } = new();
}
```

## ISetting Interface

For advanced settings that need validation or custom behavior:

```csharp
public interface ISetting
{
    // Marker interface for settings classes
}

public class ValidatedSettings : ISetting
{
    private string _connectionString = "";
    
    public string ConnectionString 
    { 
        get => _connectionString;
        set 
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Connection string cannot be empty");
            _connectionString = value;
        }
    }
    
    public int MaxConnections { get; set; } = 10;
    
    // Custom validation method
    public void Validate()
    {
        if (MaxConnections <= 0)
            throw new ArgumentException("MaxConnections must be positive");
            
        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new ArgumentException("ConnectionString is required");
    }
}
```

## SettingCallbackAttribute

The `SettingCallbackAttribute` allows automatic invocation of methods when settings are loaded:

### Attribute Definition

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class SettingCallbackAttribute : Attribute
{
    // Marker attribute for callback methods
}
```

### Usage Example

```csharp
public class CallbackEnabledSettings
{
    public string DatabaseUrl { get; set; } = "";
    public bool EnableCaching { get; set; } = true;
    
    [SettingCallback]
    public void OnSettingsLoaded()
    {
        // Called automatically when settings are initialized
        Console.WriteLine("Settings loaded successfully!");
        
        // Perform validation
        if (string.IsNullOrEmpty(DatabaseUrl))
            throw new InvalidOperationException("DatabaseUrl is required");
    }
    
    [SettingCallback]
    public void ConfigureLogging()
    {
        // Additional setup method
        if (EnableCaching)
        {
            // Initialize caching
        }
    }
}
```

## Configuration Integration

### Using with Microsoft.Extensions.Configuration

```csharp
public class MyPlugin : IPlugin
{
    private MyPluginSettings _settings;
    
    public void Initialize(PluginBootstrapContext context)
    {
        // Method 1: Using SettingsManager
        var settingsManager = new SettingsManager("my-plugin-settings.json");
        var config = settingsManager.GetConfiguration<MyPluginSettings>();
        
        // Method 2: Using built-in configuration
        _settings = context.Configuration
            .GetSection("MyPlugin")
            .Get<MyPluginSettings>() ?? new MyPluginSettings();
            
        // Method 3: Manual binding
        _settings = new MyPluginSettings();
        context.Configuration.GetSection("MyPlugin").Bind(_settings);
    }
}
```

### Configuration Hierarchy

The settings system supports configuration from multiple sources in order of precedence:

1. **Command Line Arguments** (highest precedence)
2. **Environment Variables**
3. **JSON File**
4. **Default Values** (lowest precedence)

### Example Configuration Sources

**JSON File (appsettings.json):**
```json
{
  "MyPlugin": {
    "Name": "Production Plugin",
    "Database": {
      "ConnectionString": "Server=prod;Database=mydb;",
      "CommandTimeout": 60
    },
    "Api": {
      "BaseUrl": "https://prod-api.example.com",
      "Timeout": "00:01:00"
    },
    "EnabledFeatures": ["Feature1", "Feature2"]
  }
}
```

**Environment Variables:**
```bash
export MyPlugin__Database__ConnectionString="Server=test;Database=testdb;"
export MyPlugin__Api__ApiKey="test-api-key"
```

**Command Line:**
```bash
./myapp --MyPlugin:Database:ConnectionString="Server=local;Database=localdb;"
```

## Advanced Usage Examples

### Plugin with Comprehensive Settings

```csharp
public class AdvancedPlugin : IPlugin
{
    private AdvancedPluginSettings _settings;
    private SettingsManager _settingsManager;
    
    public void Initialize(PluginBootstrapContext context)
    {
        // Create settings manager for this plugin
        var settingsFile = Path.Combine(context.PluginDirectory, "settings.json");
        _settingsManager = new SettingsManager(settingsFile);
        
        // Load configuration
        var configuration = _settingsManager.GetConfiguration<AdvancedPluginSettings>();
        _settings = configuration.Get<AdvancedPluginSettings>() ?? new AdvancedPluginSettings();
        
        // Validate settings
        _settings.Validate();
        
        context.Logger.LogInformation("Plugin configured with {FeatureCount} features", 
            _settings.EnabledFeatures.Count);
    }
    
    public void RegisterServices(IServiceCollection services)
    {
        // Register settings for DI
        services.AddSingleton(_settings);
        services.AddSingleton<ISettingsManager>(_settingsManager);
        
        // Configure services based on settings
        if (_settings.Database.Enabled)
        {
            services.AddDbContext<MyDbContext>(options =>
                options.UseSqlServer(_settings.Database.ConnectionString));
        }
        
        if (_settings.Api.Enabled)
        {
            services.AddHttpClient<IApiClient, ApiClient>(client =>
            {
                client.BaseAddress = new Uri(_settings.Api.BaseUrl);
                client.Timeout = _settings.Api.Timeout;
            });
        }
    }
}

public class AdvancedPluginSettings : ISetting
{
    public string Name { get; set; } = "Advanced Plugin";
    public DatabaseSettings Database { get; set; } = new();
    public ApiSettings Api { get; set; } = new();
    public CachingSettings Caching { get; set; } = new();
    public List<string> EnabledFeatures { get; set; } = new();
    
    [SettingCallback]
    public void OnLoaded()
    {
        Console.WriteLine($"Loaded settings for {Name}");
    }
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Plugin name is required");
            
        if (Database.Enabled && string.IsNullOrWhiteSpace(Database.ConnectionString))
            throw new ArgumentException("Database connection string is required when database is enabled");
            
        if (Api.Enabled && string.IsNullOrWhiteSpace(Api.BaseUrl))
            throw new ArgumentException("API base URL is required when API is enabled");
    }
}

public class DatabaseSettings
{
    public bool Enabled { get; set; } = false;
    public string ConnectionString { get; set; } = "";
    public int CommandTimeout { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
}

public class ApiSettings
{
    public bool Enabled { get; set; } = false;
    public string BaseUrl { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}

public class CachingSettings
{
    public bool Enabled { get; set; } = true;
    public TimeSpan DefaultExpiry { get; set; } = TimeSpan.FromMinutes(10);
    public int MaxItems { get; set; } = 1000;
}
```

### Settings with Dynamic Updates

```csharp
public class DynamicSettingsPlugin : IPlugin
{
    private IConfiguration _configuration;
    private IConfigurationRoot _configRoot;
    
    public void Initialize(PluginBootstrapContext context)
    {
        var settingsFile = Path.Combine(context.PluginDirectory, "dynamic-settings.json");
        
        _configRoot = new ConfigurationBuilder()
            .SetBasePath(context.PluginDirectory)
            .AddJsonFile("dynamic-settings.json", optional: false, reloadOnChange: true)
            .Build();
            
        _configuration = _configRoot;
        
        // Subscribe to configuration changes
        _configRoot.GetReloadToken().RegisterChangeCallback(OnConfigurationChanged, null);
    }
    
    private void OnConfigurationChanged(object? state)
    {
        Console.WriteLine("Configuration changed, reloading settings...");
        
        // Reload settings
        var newSettings = _configuration.Get<DynamicSettings>() ?? new DynamicSettings();
        
        // Apply new settings
        ApplySettings(newSettings);
        
        // Register for next change
        _configRoot.GetReloadToken().RegisterChangeCallback(OnConfigurationChanged, null);
    }
    
    private void ApplySettings(DynamicSettings settings)
    {
        // Apply the new configuration
        Console.WriteLine($"Applied new settings: MaxItems={settings.MaxItems}");
    }
}
```

## Best Practices

### 1. Settings Organization
- Group related settings into nested classes
- Use meaningful property names
- Provide sensible default values

### 2. Validation
- Implement validation in settings classes
- Use the `ISetting` interface for complex settings
- Validate early in plugin initialization

### 3. Security
- Never store sensitive data in plain text
- Use environment variables for secrets
- Consider encryption for sensitive configuration files

### 4. File Management
- Store plugin settings in the plugin directory
- Use consistent naming conventions
- Handle file access errors gracefully

### 5. Performance
- Cache configuration values when possible
- Avoid frequent file reads
- Use reload tokens for dynamic updates

### 6. Documentation
- Document all configuration options
- Provide example configuration files
- Document environment variable names

## Common Patterns

### Environment-Specific Settings

```csharp
public class EnvironmentAwareSettings
{
    public string Environment { get; set; } = "Development";
    public DatabaseSettings Development { get; set; } = new();
    public DatabaseSettings Production { get; set; } = new();
    
    public DatabaseSettings GetCurrentEnvironmentSettings()
    {
        return Environment.ToLower() switch
        {
            "production" => Production,
            "development" => Development,
            _ => Development
        };
    }
}
```

### Feature Flags

```csharp
public class FeatureFlagSettings
{
    public Dictionary<string, bool> Features { get; set; } = new()
    {
        ["NewDashboard"] = false,
        ["BetaApi"] = false,
        ["EnhancedLogging"] = true
    };
    
    public bool IsFeatureEnabled(string featureName)
    {
        return Features.TryGetValue(featureName, out var enabled) && enabled;
    }
}
```

### Typed Configuration Sections

```csharp
public static class ConfigurationExtensions
{
    public static T GetPluginSettings<T>(this IConfiguration configuration, string pluginName) 
        where T : new()
    {
        return configuration.GetSection($"Plugins:{pluginName}").Get<T>() ?? new T();
    }
}

// Usage
var settings = context.Configuration.GetPluginSettings<MyPluginSettings>("MyPlugin");
```