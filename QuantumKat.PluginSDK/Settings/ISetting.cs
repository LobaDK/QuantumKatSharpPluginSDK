using QuantumKat.PluginSDK.Attributes;

namespace QuantumKat.PluginSDK;

/// <summary>
/// Represents a settings interface for plugins.
/// This interface is used to define settings that can be configured for a plugin.
/// The settings will be added to the configuration system of the bot. Only the root section should implement this interface.
/// </summary>
public interface ISetting
{
    /// <summary>
    /// Gets the version of the settings.
    /// This version is used to determine if the settings have changed and if they need to be upgraded.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the name of the section in the configuration file.
    /// This name is used to identify the settings for the plugin in the configuration system.
    /// The section name should be unique to the plugin and should not conflict with other plugins.
    /// </summary>
    string SectionName { get; }

    /// <summary>
    /// Gets the default settings for the plugin.
    /// This method is used to provide an instance of the settings with default values.
    /// The default settings will be used to initialize the settings in the configuration system.
    /// If complex default values are needed, each property can be labeled with the <see cref="SettingCallbackAttribute"/> attribute
    /// </summary>
    object GetDefaultSettings();
}
