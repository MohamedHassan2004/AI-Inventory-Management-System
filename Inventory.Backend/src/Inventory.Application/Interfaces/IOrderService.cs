using Inventory.Application.DTOs.Order;
using Inventory.Domain.Shared;

namespace Inventory.Application.Interfaces
{
    public interface IOrderService
    {
        Task<Result<OrderResponseDto>> SubmitAsync(
            string userId,
            SubmitOrderDto dto,
            CancellationToken cancellationToken = default);
    }
}