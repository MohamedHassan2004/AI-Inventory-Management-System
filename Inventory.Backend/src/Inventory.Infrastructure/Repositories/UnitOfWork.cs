using Inventory.Domain.Interfaces;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _dbContext;
    public ICategoryRepository Categories { get; private set; }

    public UnitOfWork(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;

        Categories = new CategoryRepository(_dbContext);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}