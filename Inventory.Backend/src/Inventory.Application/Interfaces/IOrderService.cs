using Inventory.Application.DTOs.Order;
using Inventory.Domain.Shared;

namespace Inventory.Application.Interfaces
{
    public interface IOrderService
    {
        Task<Result<DetailedOrderResponseDto>> SubmitAsync(
            string userId,
            SubmitOrderDto dto,
            CancellationToken cancellationToken = default);

        Task<Result<OrderResponseDto>> CreateDraftAsync(
            string cashierId, 
            CancellationToken cancellationToken = default);

        Task<Result<OrderResponseDto>> AddItemAsync(
            string cashierId, 
            int orderId, 
            AddOrderItemDto dto, 
            CancellationToken cancellationToken = default);

        Task<Result<OrderResponseDto>> RemoveItemAsync(
            string cashierId, 
            int orderId, 
            int productId, 
            CancellationToken cancellationToken = default);

        Task<Result<OrderResponseDto>> UpdateItemQuantityAsync(
            string cashierId, 
            int orderId, 
            int productId, 
            decimal quantity, 
            CancellationToken cancellationToken = default);

        Task<Result<DetailedOrderResponseDto>> ConfirmOrderAsync(
            string cashierId, 
            int orderId, 
            ConfirmOrderDto dto, 
            CancellationToken cancellationToken = default);

        Task<Result> CancelDraftAsync(
            string cashierId, 
            int orderId, 
            CancellationToken cancellationToken = default);

        Task<Result<DetailedOrderResponseDto>> MarkAsDeliveredAsync(
            string cashierId,
            int orderId,
            CancellationToken cancellationToken = default);

        Task<Result<DetailedOrderResponseDto>> FailDeliveryAsync(
            string cashierId,
            int orderId,
            CancellationToken cancellationToken = default);
    }
}