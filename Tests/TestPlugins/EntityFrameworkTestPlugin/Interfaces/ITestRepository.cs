using Tests.TestPlugins.EntityFrameworkTestPlugin.Models;

namespace Tests.TestPlugins.EntityFrameworkTestPlugin.Interfaces;

public interface ITestRepository
{
    Task AddTestEntityAsync(TestEntityModel entity);
    Task<TestEntityModel?> GetTestEntityByIdAsync(int id);
    Task<TestEntityModel> UpdateTestEntityAsync(TestEntityModel entity);
}
