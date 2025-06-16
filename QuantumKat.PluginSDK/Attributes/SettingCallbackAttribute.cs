namespace QuantumKat.PluginSDK.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class SettingCallbackAttribute : Attribute
{
    public string MethodName { get; }

    public SettingCallbackAttribute(string methodName)
    {
        MethodName = methodName;
    }
}
