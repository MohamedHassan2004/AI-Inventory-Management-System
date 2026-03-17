using System.Linq.Expressions;

namespace Inventory.Domain.Interfaces;

public interface IRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Delete(T entity);
    void DeleteRange(IEnumerable<T> values);
    void Update(T entity);
}
