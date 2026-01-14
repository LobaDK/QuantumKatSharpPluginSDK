using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuantumKat.PluginSDK.Core;
using QuantumKat.PluginSDK.Core.Interfaces;
using Tests.TestPlugins.EntityFrameworkTestPlugin.Interfaces;
using Tests.TestPlugins.EntityFrameworkTestPlugin.Repositories;

namespace Tests.TestPlugins.EntityFrameworkTestPlugin;

public class EntityFrameworkTestPlugin : IPlugin
{
    public string Name => "EntityFrameworkTestPlugin";
    public string Description => "A test plugin that uses Entity Framework Core with an in-memory database";
    public string Version => "1.0.0";
    public string Author => "Test Suite";
    public Dictionary<string, List<string>> PluginDependencies => [];

    public void Initialize(PluginBootstrapContext context)
    {
        // Initialize the plugin
    }

    public void RegisterServices(IServiceCollection services)
    {
        // Register the DbContext with an in-memory database
        services.AddDbContext<Context>(options =>
        {
            options.UseInMemoryDatabase("EntityFrameworkTestPluginDb");
        });

        services.AddScoped<ITestRepository, TestRepository>();
    }
}
