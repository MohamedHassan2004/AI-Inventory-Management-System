using Inventory.Application.DTOs.Supplier;
using Inventory.Domain.Shared;

namespace Inventory.Application.Interfaces
{
    public interface ISupplierService
    {
        Task<Result<SupplierDto>> GetSupplierByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<SupplierDto>>> GetAllSuppliersAsync(CancellationToken cancellationToken = default);
        Task<Result<SupplierDto>> CreateSupplierAsync(CreateSupplierDto dto, CancellationToken cancellationToken = default);
        Task<Result<SupplierDto>> UpdateSupplierAsync(UpdateSupplierDto dto, CancellationToken cancellationToken = default);
        Task<Result> DeleteSupplierAsync(int id, CancellationToken cancellationToken = default);
    }
}