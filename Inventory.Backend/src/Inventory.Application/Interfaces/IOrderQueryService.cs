using Inventory.Application.DTOs.Order;
using Inventory.Domain.Shared;

namespace Inventory.Application.Interfaces
{
    public interface IOrderQueryService
    {
        Task<Result<DetailedOrderResponseDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<Result<PagedResult<OrderResponseDto>>> GetAllAsync(OrderFilter filter, CancellationToken cancellationToken = default);

        Task<Result<IEnumerable<OrderItemResponseDto>>> GetItemsByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);
    }
}