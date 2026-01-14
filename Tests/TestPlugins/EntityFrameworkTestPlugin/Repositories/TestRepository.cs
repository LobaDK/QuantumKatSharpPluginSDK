using Microsoft.EntityFrameworkCore;
using Tests.TestPlugins.EntityFrameworkTestPlugin.Interfaces;
using Tests.TestPlugins.EntityFrameworkTestPlugin.Models;

namespace Tests.TestPlugins.EntityFrameworkTestPlugin.Repositories;

public class TestRepository(Context context) : ITestRepository
{
    private readonly Context _context = context;

    public async Task AddTestEntityAsync(TestEntityModel entityModel)
    {
        await _context.TestEntities.AddAsync(entityModel);
        await _context.SaveChangesAsync();
    }

    public async Task<TestEntityModel?> GetTestEntityByIdAsync(int id)
    {
        return await _context.TestEntities.SingleOrDefaultAsync(entity => entity.Id == id);
    }

    public async Task<TestEntityModel> UpdateTestEntityAsync(TestEntityModel entityModel)
    {
        _context.TestEntities.Update(entityModel);
        await _context.SaveChangesAsync();
        return entityModel;
    }
}
