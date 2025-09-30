using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuantumKat.PluginSDK.Core;
using QuantumKat.PluginSDK.Core.Extensions;
using QuantumKat.PluginSDK.Core.Interfaces;

namespace Tests;

public class DependencyInjectionTests
{
    private interface ITestMainService
    {
        string GetMessage();
    }

    private class TestMainService : ITestMainService
    {
        public string GetMessage() => "Hello from main service";
    }

    private interface ITestPluginService
    {
        string GetPluginMessage();
    }

    private class TestPluginService(ITestMainService mainService) : ITestPluginService
    {
        private readonly ITestMainService _mainService = mainService;

        public string GetPluginMessage() => $"Plugin received: {_mainService.GetMessage()}";
    }

    [Fact]
    public void SharedServiceProvider_ShouldShareServicesBetweenMainAppAndPlugins()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITestMainService, TestMainService>();

        var configuration = new ConfigurationBuilder().Build();
        var logger = new LoggerFactory().CreateLogger<DependencyInjectionTests>();

        var sharedServiceProvider = new SharedServiceProvider(services);

        // Act - Register plugin services
        sharedServiceProvider.RegisterServicesAndRebuild(pluginServices =>
        {
            pluginServices.AddTransient<ITestPluginService, TestPluginService>();
        });

        var serviceProvider = sharedServiceProvider.ServiceProvider;

        // Assert - Both main and plugin services should be available
        var mainService = serviceProvider.GetRequiredService<ITestMainService>();
        var pluginService = serviceProvider.GetRequiredService<ITestPluginService>();

        Assert.NotNull(mainService);
        Assert.NotNull(pluginService);
        Assert.Equal("Hello from main service", mainService.GetMessage());
        Assert.Equal("Plugin received: Hello from main service", pluginService.GetPluginMessage());
    }

    [Fact]
    public void ServiceCollectionExtensions_ShouldIntegrateCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ITestMainService, TestMainService>();

        var configuration = new ConfigurationBuilder().Build();
        var logger = new LoggerFactory().CreateLogger<DependencyInjectionTests>();

        // Act
        var builder = services.AddPluginSDK(configuration, logger);
        
        builder.ConfigureServices(s =>
        {
            s.AddTransient<ITestPluginService, TestPluginService>();
        });

        var serviceProvider = builder.Build();

        // Assert
        var mainService = serviceProvider.GetRequiredService<ITestMainService>();
        var pluginService = serviceProvider.GetRequiredService<ITestPluginService>();
        var pluginServiceProvider = serviceProvider.GetRequiredService<IPluginServiceProvider>();
        var pluginManager = serviceProvider.GetRequiredService<PluginManager>();

        Assert.NotNull(mainService);
        Assert.NotNull(pluginService);
        Assert.NotNull(pluginServiceProvider);
        Assert.NotNull(pluginManager);
        Assert.Equal("Plugin received: Hello from main service", pluginService.GetPluginMessage());
    }

    [Fact]
    public void SharedServiceProvider_ShouldRebuildCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ITestMainService, TestMainService>();
        
        var sharedServiceProvider = new SharedServiceProvider(services);
        
        // First build - verify it doesn't have plugin service initially
        var serviceProvider1 = sharedServiceProvider.ServiceProvider;
        var hasPluginService1 = serviceProvider1.GetService<ITestPluginService>() != null;
        
        // Act - Register new services and rebuild
        sharedServiceProvider.RegisterServicesAndRebuild(s =>
        {
            s.AddTransient<ITestPluginService, TestPluginService>();
        });
        
        var serviceProvider2 = sharedServiceProvider.ServiceProvider;
        
        // Assert - Should be different instances and new provider should have all services
        Assert.NotSame(serviceProvider1, serviceProvider2);
        
        // First build should not have had plugin service
        Assert.False(hasPluginService1);
        
        // Second provider should have both services
        Assert.NotNull(serviceProvider2.GetRequiredService<ITestMainService>());
        Assert.NotNull(serviceProvider2.GetRequiredService<ITestPluginService>());
    }
}