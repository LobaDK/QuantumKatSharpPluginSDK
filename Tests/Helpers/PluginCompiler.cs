using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using QuantumKat.PluginSDK.Core.Interfaces;

namespace Tests.Helpers;

/// <summary>
/// Builds temporary plugin assemblies for tests, with optional dependency declarations.
/// </summary>
public static class PluginCompiler
{
    /// <summary>
    /// Creates a temporary plugin assembly implementing <see cref="IPlugin"/>.
    /// </summary>
    /// <param name="name">Plugin name.</param>
    /// <param name="version">Plugin version.</param>
    /// <param name="description">Plugin description.</param>
    /// <param name="author">Plugin author.</param>
    /// <param name="dependencies">Optional dependency map.</param>
    /// <returns>Full path to the emitted plugin assembly.</returns>
    public static string CreatePluginAssembly(
        string name,
        string version,
        string? description = null,
        string? author = null,
        Dictionary<string, List<string>>? dependencies = null)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "PluginTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var assemblyName = name + "_" + Guid.NewGuid().ToString("N");
        var dllPath = Path.Combine(tempDir, assemblyName + ".dll");

        var depLiteral = dependencies == null || dependencies.Count == 0
            ? "new Dictionary<string, List<string>>()"
            : RenderDependencyLiteral(dependencies);

        var src = string.Join('\n', new[]
        {
            "using System;",
            "using System.Collections.Generic;",
            "using Microsoft.Extensions.DependencyInjection;",
            "using QuantumKat.PluginSDK.Core;",
            "using QuantumKat.PluginSDK.Core.Interfaces;",
            "namespace DynamicTestPlugins {",
            $"    public class {name} : IPlugin {{",
            $"        public string Name => \"{name}\";",
            $"        public string Description => \"{description ?? "Dynamic test plugin"}\";",
            $"        public string Version => \"{version}\";",
            $"        public string Author => \"{author ?? "Tests"}\";",
            $"        public Dictionary<string, List<string>> PluginDependencies => {depLiteral};",
            "        public void Initialize(PluginBootstrapContext context) { }",
            $"        public void RegisterServices(IServiceCollection services) => services.AddSingleton<{name}Marker>();",
            "    }",
            $"    public class {name}Marker {{ }}",
            "}"
        });

        var syntaxTree = CSharpSyntaxTree.ParseText(src);
        var refs = GetMetadataReferences();
        var compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: new[] { syntaxTree },
            references: refs,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var emitResult = compilation.Emit(dllPath);
        if (!emitResult.Success)
        {
            var diag = string.Join(Environment.NewLine, emitResult.Diagnostics.Select(d => d.ToString()));
            throw new InvalidOperationException("Failed to build test plugin: " + diag);
        }

        return dllPath;
    }

    public static string CreateBadPluginAssembly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "PluginTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var assemblyName = "BadPlugin_" + Guid.NewGuid().ToString("N");
        var dllPath = Path.Combine(tempDir, assemblyName + ".dll");

        var src = "This is not valid C# code!";

        var syntaxTree = CSharpSyntaxTree.ParseText(src);
        var refs = GetMetadataReferences();
        var compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: new[] { syntaxTree },
            references: refs,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var emitResult = compilation.Emit(dllPath);
        if (emitResult.Success)
        {
            throw new InvalidOperationException("Bad plugin assembly was unexpectedly built successfully.");
        }

        return dllPath;
    }

    private static string RenderDependencyLiteral(Dictionary<string, List<string>> dependencies)
    {
        var sb = new StringBuilder("new Dictionary<string, List<string>>{");
        var first = true;
        foreach (var dep in dependencies)
        {
            if (!first)
            {
                sb.Append(',');
            }
            sb.Append("{\"").Append(dep.Key).Append("\", new List<string>{");
            sb.Append(string.Join(",", dep.Value.Select(v => "\"" + v + "\"")));
            sb.Append("}}");
            first = false;
        }
        sb.Append('}');
        return sb.ToString();
    }

    private static List<PortableExecutableReference> GetMetadataReferences()
    {
        // Core framework assemblies
        var tpa = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;
        var refs = tpa.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToList();

        // Ensure Plugin SDK and DI assemblies are included
        var sdkAssembly = typeof(IPlugin).Assembly.Location;
        refs.Add(MetadataReference.CreateFromFile(sdkAssembly));

        // Include assemblies referenced by the SDK to satisfy type resolution
        var sdkRefAssemblies = typeof(IPlugin).Assembly
            .GetReferencedAssemblies()
            .Select(Assembly.Load)
            .Select(a => a.Location)
            .Distinct();
        foreach (var loc in sdkRefAssemblies)
        {
            if (!string.IsNullOrWhiteSpace(loc))
            {
                refs.Add(MetadataReference.CreateFromFile(loc));
            }
        }

        return refs;
    }
}
