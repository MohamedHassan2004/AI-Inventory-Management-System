using Inventory.Application.DTOs.ReturnOrder;
using Inventory.Domain.Shared;

namespace Inventory.Application.Interfaces
{
    public interface IReturnOrderQueryService
    {
        Task<Result<ReturnOrderResponseDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<PagedResult<ReturnOrderResponseDto>>> GetAllAsync(ReturnOrderFilter filter, CancellationToken cancellationToken = default);
    }
}
