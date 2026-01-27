using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuantumKat.PluginSDK.Core.Extensions;

namespace Tests;

public class ServiceCollectionExtensionsTests
{

    [Fact]
    public void AddPluginServices_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        var logger = new LoggerFactory().CreateLogger("TestLogger");

        // Act
        var result = services.AddPluginSDK(configuration, logger);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(services);
    }
}
