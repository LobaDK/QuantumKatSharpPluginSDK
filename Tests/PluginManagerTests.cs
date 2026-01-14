using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using QuantumKat.PluginSDK.Core;
using QuantumKat.PluginSDK.Core.Interfaces;
using Tests.Helpers;

namespace Tests;

public class PluginManagerTests
{
    private readonly Mock<IPluginServiceProvider> _serviceProviderMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();
    private readonly ILogger<PluginManager> _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<PluginManager>();

    /// <summary>
    /// Gets or builds the paths to the test plugin assemblies.
    /// </summary>
    private static IEnumerable<string> GetTestPluginPaths()
    {
        var testProjectDir = GetTestProjectDirectory();
        return [
            Path.Combine(testProjectDir, "TestPlugins", "SimpleTestPlugin", "bin", "Debug", "net8.0", "SimpleTestPlugin.dll"),
            Path.Combine(testProjectDir, "TestPlugins", "ThreadedTestPlugin", "bin", "Debug", "net8.0", "ThreadedTestPlugin.dll")
        ];
    }

    /// <summary>
    /// Gets the test project directory.
    /// </summary>
    private static string GetTestProjectDirectory()
    {
        var currentDir = AppContext.BaseDirectory;
        while (currentDir != null && !File.Exists(Path.Combine(currentDir, "Tests.csproj")))
        {
            currentDir = Path.GetDirectoryName(currentDir);
        }
        return currentDir ?? throw new InvalidOperationException("Could not find Tests.csproj");
    }

    [Theory]
    [InlineData("1.2.3", "", true)]
    [InlineData("1.2.3", null, true)]
    [InlineData("1.2.3", "==1.2.3", true)]
    [InlineData("1.2.3", "==1.2.4", false)]
    [InlineData("1.2.3", ">1.2.2", true)]
    [InlineData("1.2.3", ">1.2.3", false)]
    [InlineData("1.2.3", "<1.2.4", true)]
    [InlineData("1.2.3", "<1.2.3", false)]
    [InlineData("1.2.3", ">=1.2.3", true)]
    [InlineData("1.2.3", ">=1.2.2", true)]
    [InlineData("1.2.3", ">=1.2.4", false)]
    [InlineData("1.2.3", "<=1.2.3", true)]
    [InlineData("1.2.3", "<=1.2.4", true)]
    [InlineData("1.2.3", "<=1.2.2", false)]
    [InlineData("1.2.3", "1.2.3", true)]
    [InlineData("1.2.3", "1.2.4", false)]
    public void CheckVersion_VariousCases_ReturnsExpected(string actual, string requirement, bool expected)
    {
        bool result = PluginManager.CheckVersion(actual, requirement);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CheckVersion_WhitespaceRequirement_ReturnsTrue()
    {
        Assert.True(PluginManager.CheckVersion("1.2.3", "   "));
    }

    [Fact]
    public void CheckVersion_UnknownOperator_TreatedAsExact()
    {
        Assert.False(PluginManager.CheckVersion("1.2.3", "!=1.2.3"));
    }

    [Fact]
    public void LoadPlugin_NonExistentPath_IsIgnored()
    {
        var pluginManager = new PluginManager(_serviceProviderMock.Object, _configurationMock.Object, _logger);
        pluginManager.LoadPlugins(["nonexistent.dll"]);
    }

    [Fact]
    public void LoadPlugin_InvalidAssembly()
    {
        var badAssemblyPath = PluginCompiler.CreateBadPluginAssembly();

        var pluginManager = new PluginManager(_serviceProviderMock.Object, _configurationMock.Object, _logger);
        
        // should gracefully handle bad assembly when not throwing
        pluginManager.LoadPlugins([badAssemblyPath], false);
        
        // should throw when configured to do so
        Assert.Throws<BadImageFormatException>(() => pluginManager.LoadPlugins([badAssemblyPath], true));
    }

    /// <summary>
    /// Tests that actual test plugins can be loaded when they exist.
    /// This requires the test plugins to be built first.
    /// </summary>
    [Fact]
    public void LoadPlugins_WithValidTestPlugins_LoadsSuccessfully()
    {
        var testPluginPaths = GetTestPluginPaths();

        // Skip test if plugins haven't been built yet
        if (!testPluginPaths.All(File.Exists))
        {
            return;
        }

        var serviceCollection = new ServiceCollection();
        _serviceProviderMock.Setup(sp => sp.ServiceProvider)
            .Returns(serviceCollection.BuildServiceProvider());

        var pluginManager = new PluginManager(_serviceProviderMock.Object, _configurationMock.Object, _logger);

        // Should not throw
        pluginManager.LoadPlugins(testPluginPaths);
    }

    /// <summary>
    /// Tests registering services from loaded test plugins.
    /// </summary>
    [Fact]
    public void RegisterAllPluginServices_WithLoadedPlugins_RegistersSuccessfully()
    {
        var testPluginPaths = GetTestPluginPaths();

        // Skip test if plugins haven't been built yet
        if (!testPluginPaths.All(File.Exists))
        {
            return;
        }

        var serviceCollection = new ServiceCollection();
        var sharedServiceProvider = new SharedServiceProvider(serviceCollection);

        var pluginManager = new PluginManager(sharedServiceProvider, _configurationMock.Object, _logger);

        pluginManager.LoadPlugins(testPluginPaths);

        // Should not throw and should rebuild the service provider
        pluginManager.RegisterAllPluginServices();
    }

    [Fact]
    public void LoadPlugins_WithDynamicDependentPlugin_ResolvesDependencies()
    {
        var testPluginPaths = GetTestPluginPaths().ToList();

        // Skip test if base plugins haven't been built yet
        if (!testPluginPaths.All(File.Exists))
        {
            return;
        }

        var dynamicPluginPath = PluginCompiler.CreatePluginAssembly(
            name: "DependentTestPlugin",
            version: "1.0.0",
            dependencies: new Dictionary<string, List<string>>
            {
                { "SimpleTestPlugin", new() { "==1.0.0" } }
            },
            description: "Dynamic dependent test plugin"
        );

        testPluginPaths.Add(dynamicPluginPath);

        var serviceCollection = new ServiceCollection();
        var sharedServiceProvider = new SharedServiceProvider(serviceCollection);

        var pluginManager = new PluginManager(sharedServiceProvider, _configurationMock.Object, _logger);

        pluginManager.LoadPlugins(testPluginPaths, true);
        pluginManager.RegisterAllPluginServices();

        Assert.Contains(pluginManager.LoadedPlugins, p => p.Name == "DependentTestPlugin");
    }

    [Fact]
    public void LoadPlugins_HandlesCircularDependencies()
    {
        var dynamicPluginAPath = PluginCompiler.CreatePluginAssembly(
            name: "CircularPluginA",
            version: "1.0.0",
            dependencies: new Dictionary<string, List<string>>
            {
        { "CircularPluginB", new() { "==1.0.0" } }
            },
            description: "Circular dependency plugin A"
        );

        var dynamicPluginBPath = PluginCompiler.CreatePluginAssembly(
            name: "CircularPluginB",
            version: "1.0.0",
            dependencies: new Dictionary<string, List<string>>
            {
                { "CircularPluginA", new() { "==1.0.0" } }
            },
            description: "Circular dependency plugin B"
        );

        List<string> testPluginPaths = [dynamicPluginAPath, dynamicPluginBPath];

        var serviceCollection = new ServiceCollection();
        var sharedServiceProvider = new SharedServiceProvider(serviceCollection);

        var pluginManager = new PluginManager(sharedServiceProvider, _configurationMock.Object, _logger);

        // Should gracefully handle circular dependency when not throwing
        pluginManager.LoadPlugins(testPluginPaths, false);
        
        // Should throw when configured to do so
        Assert.Throws<InvalidOperationException>(() => pluginManager.LoadPlugins(testPluginPaths, true));
    }

    [Fact]
    public void RegisterAllPluginServices_NoPlugins_DoesNotThrow()
    {
        var serviceCollection = new ServiceCollection();
        var sharedServiceProvider = new SharedServiceProvider(serviceCollection);

        var pluginManager = new PluginManager(sharedServiceProvider, _configurationMock.Object, _logger);

        // Should not throw even with no plugins loaded
        pluginManager.RegisterAllPluginServices();
    }

    [Fact]
    public async Task RegisteredThreadedPlugins_StartAndStopSuccessfully()
    {
        var testPluginPaths = GetTestPluginPaths();

        // Skip test if plugins haven't been built yet
        if (!testPluginPaths.All(File.Exists))
        {
            return;
        }

        var serviceCollection = new ServiceCollection();
        var sharedServiceProvider = new SharedServiceProvider(serviceCollection);

        var pluginManager = new PluginManager(sharedServiceProvider, _configurationMock.Object, _logger);

        pluginManager.LoadPlugins(testPluginPaths, true);
        pluginManager.RegisterAllPluginServices();

        var cancellationToken = CancellationToken.None;

        // Start all threaded plugins
        var startTask = pluginManager.StartAllPluginsAsync(cancellationToken);
        await startTask;

        var threadedPlugins = pluginManager.LoadedPlugins
            .OfType<IThreadedPlugin>()
            .ToList();

        foreach (var plugin in threadedPlugins)
        {
            Assert.True(plugin.IsRunning);
        }

        // Stop all threaded plugins
        var stopTask = pluginManager.StopAllPluginsAsync(cancellationToken);
        await stopTask;

        foreach (var plugin in threadedPlugins)
        {
            Assert.False(plugin.IsRunning);
        }
    }

    [Fact]
    public async Task RegisteredThreadedPlugins_IndividualStartAndStopSuccessfully()
    {
        var testPluginPaths = GetTestPluginPaths();

        // Skip test if plugins haven't been built yet
        if (!testPluginPaths.All(File.Exists))
        {
            return;
        }

        var serviceCollection = new ServiceCollection();
        var sharedServiceProvider = new SharedServiceProvider(serviceCollection);

        var pluginManager = new PluginManager(sharedServiceProvider, _configurationMock.Object, _logger);

        pluginManager.LoadPlugins(testPluginPaths, true);
        pluginManager.RegisterAllPluginServices();

        var cancellationToken = CancellationToken.None;

        var threadedPlugins = pluginManager.LoadedPlugins
            .OfType<IThreadedPlugin>()
            .ToList();

        foreach (var plugin in threadedPlugins)
        {
            // Start individual plugin
            await plugin.StartAsync(cancellationToken);
            Assert.True(plugin.IsRunning);

            // Stop individual plugin
            await plugin.StopAsync(cancellationToken);
            Assert.False(plugin.IsRunning);
        }
    }
}