namespace Inventory.Domain.Interfaces;

public interface IUnitOfWork
{
    ICategoryRepository Categories { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
