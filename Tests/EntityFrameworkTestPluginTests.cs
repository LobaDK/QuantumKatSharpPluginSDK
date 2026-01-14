using Microsoft.Extensions.DependencyInjection;
using Tests.TestPlugins.EntityFrameworkTestPlugin;
using Tests.TestPlugins.EntityFrameworkTestPlugin.Interfaces;
using Tests.TestPlugins.EntityFrameworkTestPlugin.Models;

namespace Tests;

/// <summary>
/// Contains tests for the EntityFramework test plugin functionality.
/// </summary>
public class EntityFrameworkTestPluginTests
{
    /// <summary>
    /// Tests that the plugin can successfully add an entity to the database and retrieve it.
    /// </summary>
    [Fact]
    public async Task Plugin_CanAddAndRetrieveEntity()
    {
        // Arrange
        ServiceCollection services = new();
        EntityFrameworkTestPlugin plugin = new();
        plugin.RegisterServices(services);
        
        using var serviceProvider = services.BuildServiceProvider();

        var repository = serviceProvider.GetRequiredService<ITestRepository>();

        var testEntity = new TestEntityModel
        {
            Id = 1,
            Name = "Test Entity"
        };

        // Act
        await repository.AddTestEntityAsync(testEntity);
        var retrievedEntity = await repository.GetTestEntityByIdAsync(1);

        // Assert
        Assert.NotNull(retrievedEntity);
        Assert.Equal(testEntity.Id, retrievedEntity.Id);
        Assert.Equal(testEntity.Name, retrievedEntity.Name);
    }

    /// <summary>
    /// Tests that the plugin can add multiple entities to the database and retrieve them all.
    /// </summary>
    [Fact]
    public async Task Plugin_CanAddAndRetrieveMultipleEntities()
    {
        // Arrange
        ServiceCollection services = new();
        EntityFrameworkTestPlugin plugin = new();
        plugin.RegisterServices(services);
        using var serviceProvider = services.BuildServiceProvider();

        var repository = serviceProvider.GetRequiredService<ITestRepository>();

        var entities = new List<TestEntityModel>
        {
            new() { Id = 2, Name = "Entity 1" },
            new() { Id = 3, Name = "Entity 2" },
            new() { Id = 4, Name = "Entity 3" }
        };

        // Act
        foreach (var entity in entities)
        {
            await repository.AddTestEntityAsync(entity);
        }

        // Assert
        foreach (var entity in entities)
        {
            var retrievedEntity = await repository.GetTestEntityByIdAsync(entity.Id);
            Assert.NotNull(retrievedEntity);
            Assert.Equal(entity.Id, retrievedEntity.Id);
            Assert.Equal(entity.Name, retrievedEntity.Name);
        }
    }

    /// <summary>
    /// Tests that attempting to retrieve a non-existent entity returns null.
    /// </summary>
    [Fact]
    public async Task Plugin_GetNonExistentEntity_ReturnsNull()
    {
        // Arrange
        ServiceCollection services = new();
        EntityFrameworkTestPlugin plugin = new();
        plugin.RegisterServices(services);
        using var serviceProvider = services.BuildServiceProvider();

        var repository = serviceProvider.GetRequiredService<ITestRepository>();

        // Act
        TestEntityModel? testEntity = await repository.GetTestEntityByIdAsync(-1);

        // Assert
        Assert.Null(testEntity);
    }

    /// <summary>
    /// Tests that entity updates are properly persisted to the database.
    /// </summary>
    [Fact]
    public async Task Plugin_UpdateEntity_ChangesArePersisted()
    {
        // Arrange
        ServiceCollection services = new();
        EntityFrameworkTestPlugin plugin = new();
        plugin.RegisterServices(services);
        using var serviceProvider = services.BuildServiceProvider();

        var repository = serviceProvider.GetRequiredService<ITestRepository>();

        // Act
        TestEntityModel testEntity = new() { Id = 5, Name = "Original Name" };

        await repository.AddTestEntityAsync(testEntity);

        TestEntityModel? updatedEntity = await repository.GetTestEntityByIdAsync(5);

        Assert.NotNull(updatedEntity);

        updatedEntity.Name = "Updated Name";

        await repository.UpdateTestEntityAsync(updatedEntity);

        TestEntityModel? retrievedEntity = await repository.GetTestEntityByIdAsync(5);

        Assert.NotNull(retrievedEntity);

        // Assert
        Assert.Equal("Updated Name", retrievedEntity.Name);
    }

    /// <summary>
    /// Tests that the DbContext is registered with scoped lifetime in dependency injection.
    /// </summary>
    [Fact]
    public void Plugin_DbContext_IsRegisteredAsScoped()
    {
        // Arrange
        ServiceCollection services = new();
        EntityFrameworkTestPlugin plugin = new();
        plugin.RegisterServices(services);
        using var serviceProvider = services.BuildServiceProvider();

        // Act
        using var scope1 = serviceProvider.CreateScope();
        using var scope2 = serviceProvider.CreateScope();

        var dbContext1 = scope1.ServiceProvider.GetRequiredService<Context>();
        var dbContext2 = scope1.ServiceProvider.GetRequiredService<Context>();
        var dbContext3 = scope2.ServiceProvider.GetRequiredService<Context>();

        // Assert
        Assert.Same(dbContext1, dbContext2); // Same scope should return same instance
        Assert.NotSame(dbContext1, dbContext3); // Different scopes should return different instances
    }

    /// <summary>
    /// Tests that the repository is registered with scoped lifetime in dependency injection.
    /// </summary>
    [Fact]
    public void Plugin_Repository_IsRegisteredAsScoped()
    {
        // Arrange
        ServiceCollection services = new();
        EntityFrameworkTestPlugin plugin = new();
        plugin.RegisterServices(services);
        using var serviceProvider = services.BuildServiceProvider();

        // Act
        using var scope1 = serviceProvider.CreateScope();
        using var scope2 = serviceProvider.CreateScope();

        var repo1 = scope1.ServiceProvider.GetRequiredService<ITestRepository>();
        var repo2 = scope1.ServiceProvider.GetRequiredService<ITestRepository>();
        var repo3 = scope2.ServiceProvider.GetRequiredService<ITestRepository>();

        // Assert
        Assert.Same(repo1, repo2); // Same scope should return same instance
        Assert.NotSame(repo1, repo3); // Different scopes should return different instances
    }

    /// <summary>
    /// Tests that attempting to add an entity with a duplicate ID throws an exception.
    /// </summary>
    [Fact]
    public async Task Plugin_AddDuplicateId_ThrowsException()
    {
        // Arrange
        ServiceCollection services = new();
        EntityFrameworkTestPlugin plugin = new();
        plugin.RegisterServices(services);
        using var serviceProvider = services.BuildServiceProvider();

        var repository = serviceProvider.GetRequiredService<ITestRepository>();

        // Act
        var testEntity = new TestEntityModel
        {
            Id = 10,
            Name = "First Entity"
        };

        await repository.AddTestEntityAsync(testEntity);

        // Clear the change tracker to simulate a new context
        Context context = serviceProvider.GetRequiredService<Context>();
        context.ChangeTracker.Clear();

        var duplicateEntity = new TestEntityModel
        {
            Id = 10,
            Name = "Duplicate Entity"
        };

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await repository.AddTestEntityAsync(duplicateEntity);
        });
    }
}
