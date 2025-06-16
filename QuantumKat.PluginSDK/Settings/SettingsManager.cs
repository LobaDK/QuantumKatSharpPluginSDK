using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using QuantumKat.PluginSDK.Attributes;

namespace QuantumKat.PluginSDK.Settings;

public class SettingsManager(string file)
{
    private readonly JsonSerializerOptions serializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };
    
    private readonly string _settingsFile = file;
    public IConfiguration GetConfiguration<T>() where T : new()
    {
        if (!Path.Exists(_settingsFile))
        {
            object settings = InitializeSettings<T>();
            Save(settings);
        }

        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(_settingsFile, optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddCommandLine(Environment.GetCommandLineArgs())
            .Build();
    }

    public void Save(object settings)
    {
        var json = JsonSerializer.Serialize(settings, serializerOptions);
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, _settingsFile), json);
    }

    public static T InitializeSettings<T>() where T : new()
    {
        return (T)InitializeSettings(typeof(T), null);
    }

    private static object InitializeSettings(Type type, object instance)
    {
        instance ??= Activator.CreateInstance(type);

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || !property.CanWrite)
                continue;

            var value = property.GetValue(instance);

            // If it's a nested complex object (but not string!), initialize it recursively
            if (value == null && IsComplexType(property.PropertyType))
            {
                var nestedValue = InitializeSettings(property.PropertyType, null);
                property.SetValue(instance, nestedValue);
                continue;
            }

            // Skip properties that already have a value
            if (value != null && value is IEnumerable enumerable && enumerable.GetEnumerator().MoveNext() )
                continue;

            // Check for callback
            var callbackAttr = property.GetCustomAttribute<SettingCallbackAttribute>();
            if (callbackAttr != null)
            {
                var method = type.GetMethod(callbackAttr.MethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method != null)
                {
                    var generatedValue = method.Invoke(instance, null);
                    property.SetValue(instance, generatedValue);
                }
                else
                {
                    Console.WriteLine($"Warning: Callback method '{callbackAttr.MethodName}' not found for property '{property.Name}'.");
                }
            }
        }

        return instance;
    }

    private static bool IsComplexType(Type type)
    {
        return type.IsClass && type != typeof(string);
    }
}
