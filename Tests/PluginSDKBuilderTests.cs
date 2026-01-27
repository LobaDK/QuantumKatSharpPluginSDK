using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using QuantumKat.PluginSDK.Core.Extensions;
using QuantumKat.PluginSDK.Core.Interfaces;

namespace Tests;

public class PluginSDKBuilderTests
{
    private readonly Mock<IPluginServiceProvider> _mockServiceProvider;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IServiceProvider> _mockBuiltServiceProvider;

    public PluginSDKBuilderTests()
    {
        _mockServiceProvider = new Mock<IPluginServiceProvider>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger>();
        _mockBuiltServiceProvider = new Mock<IServiceProvider>();

        _mockServiceProvider.Setup(sp => sp.ServiceProvider)
            .Returns(_mockBuiltServiceProvider.Object);
    }

    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Act
        var builder = new PluginSDKBuilder(
            _mockServiceProvider.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Assert
        Assert.NotNull(builder);
        Assert.Same(_mockServiceProvider.Object, builder.SharedServiceProvider);
    }

    [Fact]
    public void SharedServiceProvider_ReturnsCorrectInstance()
    {
        // Arrange
        var builder = new PluginSDKBuilder(
            _mockServiceProvider.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act
        var result = builder.SharedServiceProvider;

        // Assert
        Assert.Same(_mockServiceProvider.Object, result);
    }

    [Fact]
    public void LoadPlugins_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new PluginSDKBuilder(
            _mockServiceProvider.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);
        var pluginPaths = new[] { "/path/to/plugin1.dll", "/path/to/plugin2.dll" };

        // Act
        var result = builder.LoadPlugins(pluginPaths);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void Build_ReturnsServiceProvider()
    {
        // Arrange
        var builder = new PluginSDKBuilder(
            _mockServiceProvider.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act
        var result = builder.Build();

        // Assert
        Assert.Same(_mockBuiltServiceProvider.Object, result);
        _mockServiceProvider.Verify(sp => sp.ServiceProvider, Times.Once);
    }

    [Fact]
    public void ConfigureServices_WithValidAction_CallsRegisterServicesAndRebuild()
    {
        // Arrange
        var builder = new PluginSDKBuilder(
            _mockServiceProvider.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);
        Action<IServiceCollection> configureAction = services => { };

        // Act
        var result = builder.ConfigureServices(configureAction);

        // Assert
        _mockServiceProvider.Verify(
            sp => sp.RegisterServicesAndRebuild(configureAction),
            Times.Once);
        Assert.Same(builder, result);
    }

    [Fact]
    public void ConfigureServices_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new PluginSDKBuilder(
            _mockServiceProvider.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.ConfigureServices(null!));
    }

    [Fact]
    public void ConfigureServices_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new PluginSDKBuilder(
            _mockServiceProvider.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act
        var result = builder.ConfigureServices(services => 
        {
            services.AddTransient<ILogger>(_ => _mockLogger.Object);
        });

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void FluentAPI_SupportsMethodChaining()
    {
        // Arrange
        var builder = new PluginSDKBuilder(
            _mockServiceProvider.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);
        var pluginPaths = new[] { "/path/to/plugin.dll" };

        // Act
        var result = builder
            .LoadPlugins(pluginPaths)
            .ConfigureServices(services => { })
            .Build();

        // Assert
        Assert.Same(_mockBuiltServiceProvider.Object, result);
    }
}