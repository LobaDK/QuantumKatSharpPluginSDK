using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuantumKat.PluginSDK.Core;

namespace Tests;

public class SharedServiceProviderTest
{
    [Fact]
    public void Constructor_ThrowsOnNullServiceCollection()
    {
        Assert.Throws<ArgumentNullException>(() => new SharedServiceProvider(null!));
    }

    [Fact]
    public void ServiceProvider_BuildsAndReturnsProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<string>("test");
        var provider = new SharedServiceProvider(services);

        var sp = provider.ServiceProvider;

        Assert.NotNull(sp);
        Assert.Equal("test", sp.GetService<string>());
    }

    [Fact]
    public void ServiceProvider_ReturnsSameInstance()
    {
        var services = new ServiceCollection();
        var provider = new SharedServiceProvider(services);

        var sp1 = provider.ServiceProvider;
        var sp2 = provider.ServiceProvider;

        Assert.Same(sp1, sp2);
    }

    [Fact]
    public void ServiceProvider_ThrowsAfterDispose()
    {
        var services = new ServiceCollection();
        var provider = new SharedServiceProvider(services);

        provider.Dispose();

        Assert.Throws<ObjectDisposedException>(() => provider.ServiceProvider);
    }

    [Fact]
    public void RegisterServicesAndRebuild_AppliesRegistrationAndRebuilds()
    {
        var services = new ServiceCollection();
        var provider = new SharedServiceProvider(services);

        provider.RegisterServicesAndRebuild(sc => sc.AddSingleton(new ConfigurationBuilder().Build()));

        var sp = provider.ServiceProvider;
        Assert.Equal(typeof(ConfigurationRoot), sp.GetService<IConfigurationRoot>()?.GetType());
    }

    [Fact]
    public void RegisterServicesAndRebuild_ThrowsOnNullAction()
    {
        var services = new ServiceCollection();
        var provider = new SharedServiceProvider(services);

        Assert.Throws<ArgumentNullException>(() => provider.RegisterServicesAndRebuild(null!));
    }

    [Fact]
    public void RegisterServicesAndRebuild_ThrowsAfterDispose()
    {
        var services = new ServiceCollection();
        var provider = new SharedServiceProvider(services);
        provider.Dispose();

        Assert.Throws<ObjectDisposedException>(() => provider.RegisterServicesAndRebuild(sc => { }));
    }

    [Fact]
    public void RebuildServiceProvider_RebuildsProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<string>("first");
        var provider = new SharedServiceProvider(services);

        var sp1 = provider.ServiceProvider;

        provider.RegisterServicesAndRebuild(sc => sc.AddSingleton(new ConfigurationBuilder().Build()));
        provider.RebuildServiceProvider();

        var sp2 = provider.ServiceProvider;

        Assert.NotSame(sp1, sp2);
        Assert.Equal(typeof(ConfigurationRoot), sp2.GetService<IConfigurationRoot>()?.GetType());
    }

    [Fact]
    public void Dispose_DisposesProviderAndSuppressesFinalize()
    {
        var services = new ServiceCollection();
        var provider = new SharedServiceProvider(services);

        provider.Dispose();

        Assert.Throws<ObjectDisposedException>(() => provider.ServiceProvider);
    }

    [Fact]
    public void ServiceProvider_IsThreadSafe()
    {
        var services = new ServiceCollection();
        services.AddSingleton("threadsafe");
        var provider = new SharedServiceProvider(services);

        string? result = null;
        Parallel.For(0, 10, _ =>
        {
            var sp = provider.ServiceProvider;
            var value = sp.GetService<string>();
            Assert.Equal("threadsafe", value);
            result = value;
        });

        Assert.Equal("threadsafe", result);
    }
}