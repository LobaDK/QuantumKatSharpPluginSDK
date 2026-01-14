using Microsoft.EntityFrameworkCore;
using Tests.TestPlugins.EntityFrameworkTestPlugin.Models;

namespace Tests.TestPlugins.EntityFrameworkTestPlugin;

public class Context(DbContextOptions<Context> options) : DbContext(options)
{
    public DbSet<TestEntityModel> TestEntities => Set<TestEntityModel>();
}
