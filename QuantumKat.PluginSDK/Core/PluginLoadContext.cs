using System.Reflection;
using System.Runtime.Loader;

namespace QuantumKat.PluginSDK.Core;

/// <summary>
/// Represents a custom assembly load context for loading plugin assemblies.
/// </summary>
/// <remarks>
/// This context is used to load plugin assemblies from a specified path, allowing for dynamic loading
/// and isolation of plugin dependencies. It resolves assembly dependencies using the provided path.
/// </remarks>
public class PluginLoadContext(string pluginPath) : AssemblyLoadContext(true)
{
    private readonly AssemblyDependencyResolver _resolver = new(pluginPath);

    /// <summary>
    /// Loads the assembly with the specified <paramref name="assemblyName"/>.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly to load.</param>
    /// <returns>
    /// The loaded <see cref="Assembly"/> if the assembly is found; otherwise, <c>null</c>.
    /// </returns>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
    }
}