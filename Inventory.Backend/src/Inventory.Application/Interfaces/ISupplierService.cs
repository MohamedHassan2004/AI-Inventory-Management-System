using Inventory.Application.DTOs.Supplier;
using Inventory.Domain.Shared;

namespace Inventory.Application.Interfaces
{
    public interface ISupplierService
    {
        Task<Result<SupplierDto>> GetSupplierByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<SupplierDto>> CreateSupplierAsync(CreateSupplierDto dto, CancellationToken cancellationToken = default);
        Task<Result<SupplierDto>> UpdateSupplierAsync(int id, UpdateSupplierDto dto, CancellationToken cancellationToken = default);
        Task<Result> DeleteSupplierAsync(int id, CancellationToken cancellationToken = default);
        Task<Result> RestoreSupplierAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<SupplierNoteDto>>> GetSupplierNotesAsync(int supplierId, CancellationToken cancellationToken = default);
        Task<Result<SupplierDto>> AddSupplierRatingAsync(int supplierId, AddSupplierRatingDto dto, CancellationToken cancellationToken = default);
    }
}