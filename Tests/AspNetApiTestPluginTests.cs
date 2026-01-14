using Tests.TestPlugins.AspNetApiTestPlugin;
using QuantumKat.PluginSDK.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Tests;

/// <summary>
/// Tests for the AspNetApiTestPlugin that runs an ASP.NET Core API server.
/// </summary>
public class AspNetApiTestPluginTests
{
    private static PluginBootstrapContext CreateBootstrapContext()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("Test");
        
        return new PluginBootstrapContext
        {
            Configuration = config,
            Logger = logger,
            CoreServices = services.BuildServiceProvider(),
            PluginDirectory = Path.GetTempPath()
        };
    }

    [Fact]
    public async Task StartAsync_StartsServerAndCanBeContacted()
    {
        // Arrange
        var plugin = new AspNetApiTestPlugin{ ServerPort = 5556 };
        var bootstrapContext = CreateBootstrapContext();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        plugin.Initialize(bootstrapContext);

        // Act - Start the plugin
        await plugin.StartAsync(cts.Token);

        try
        {
            // Assert - Plugin should be running
            Assert.True(plugin.IsRunning, "Plugin should be running after StartAsync");

            // Give the server a moment to fully start
            await Task.Delay(500);

            // Act - Contact the health endpoint
            using var client = new HttpClient();
            var response = await client.GetAsync("http://localhost:5556/api/health");

            // Assert - Request should be successful
            Assert.True(response.IsSuccessStatusCode, $"Expected success status, got {response.StatusCode}");

            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("healthy", content);
            Assert.Contains("AspNetApiTestPlugin is running", content);
        }
        finally
        {
            // Cleanup - Stop the plugin
            await plugin.StopAsync(cts.Token);
            Assert.False(plugin.IsRunning, "Plugin should not be running after StopAsync");
        }
    }

    [Fact]
    public async Task StartAsync_TestEndpointReturnsExpectedMessage()
    {
        // Arrange
        var plugin = new AspNetApiTestPlugin{ ServerPort = 5557 };
        var bootstrapContext = CreateBootstrapContext();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        plugin.Initialize(bootstrapContext);

        // Act - Start the plugin
        await plugin.StartAsync(cts.Token);

        try
        {
            // Give the server a moment to fully start
            await Task.Delay(500);

            // Act - Contact the test endpoint
            using var client = new HttpClient();
            var response = await client.GetAsync("http://localhost:5557/api/test");

            // Assert - Request should be successful
            Assert.True(response.IsSuccessStatusCode, $"Expected success status, got {response.StatusCode}");

            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Test endpoint works!", content);
        }
        finally
        {
            // Cleanup - Stop the plugin
            await plugin.StopAsync(cts.Token);
        }
    }

    [Fact]
    public void RegisterServices_RegistersMarkerService()
    {
        // Arrange
        var plugin = new AspNetApiTestPlugin();
        var services = new ServiceCollection();

        // Act
        plugin.RegisterServices(services);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var marker = serviceProvider.GetService<AspNetApiTestPluginMarker>();
        Assert.NotNull(marker);
    }

    [Fact]
    public void IsRunning_InitiallyFalse()
    {
        // Arrange & Act
        var plugin = new AspNetApiTestPlugin();

        // Assert
        Assert.False(plugin.IsRunning, "Plugin should not be running initially");
    }

    [Fact]
    public async Task StopAsync_WhenNotRunning_DoesNotThrow()
    {
        // Arrange
        var plugin = new AspNetApiTestPlugin();
        var cts = new CancellationTokenSource();

        // Act & Assert - Should not throw
        await plugin.StopAsync(cts.Token);
    }
}
