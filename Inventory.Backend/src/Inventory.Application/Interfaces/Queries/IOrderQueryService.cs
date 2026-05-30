using Inventory.Application.DTOs.Order;
using Inventory.Domain.Shared;

namespace Inventory.Application.Interfaces.Queries
{
    public interface IOrderQueryService
    {
        Task<Result<DetailedOrderResponseDto>> GetByIdAsync(string cashierId, int id, CancellationToken cancellationToken = default);

        Task<Result<PagedResult<OrderResponseDto>>> GetAllAsync(OrderFilter filter, CancellationToken cancellationToken = default);

        Task<Result<IEnumerable<OrderItemResponseDto>>> GetItemsByOrderIdAsync(string cashierId, int orderId, CancellationToken cancellationToken = default);
    }
}
