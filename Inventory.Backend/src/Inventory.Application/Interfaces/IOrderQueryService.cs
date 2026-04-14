using Inventory.Application.DTOs.Order;
using Inventory.Domain.Shared;

namespace Inventory.Application.Interfaces
{
    public interface IOrderQueryService
    {
        Task<Result<OrderResponseDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<Result<IEnumerable<OrderResponseDto>>> GetPendingAsync(CancellationToken cancellationToken = default);

        Task<Result<IEnumerable<OrderItemResponseDto>>> GetItemsByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);

        Task<Result<IEnumerable<OrderResponseDto>>> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default);
    }
}