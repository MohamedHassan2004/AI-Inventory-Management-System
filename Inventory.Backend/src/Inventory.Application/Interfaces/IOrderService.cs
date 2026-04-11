using Inventory.Application.DTOs.Order;
using Inventory.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Interfaces
{
    public interface IOrderService
    {
        Task<Result<int>> CreateAsync(
            CreateOrderDto dto,
            string userId,
            CancellationToken cancellationToken = default);

        Task<Result> ApplyDiscountAsync(
            int orderId,
            decimal discount,
            CancellationToken cancellationToken = default);

        Task<Result> CompleteAsync(
            int orderId,
            CompleteOrderDto dto,
            CancellationToken cancellationToken = default);

        Task<Result> CancelAsync(
            int orderId,
            CancellationToken cancellationToken = default);

        Task<Result<OrderResponseDto>> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<Result<IEnumerable<OrderResponseDto>>> GetPendingOrdersAsync( CancellationToken cancellationToken = default);
        Task<Result> AddItemAsync(int orderId, OrderItemDto dto, CancellationToken cancellationToken = default);

        Task<Result> UpdateItemQuantityAsync(int orderId, int itemId, decimal quantity, CancellationToken cancellationToken = default);

        Task<Result> RemoveItemAsync(int orderId, int itemId, CancellationToken cancellationToken = default);

        Task<Result<IEnumerable<OrderItemResponseDto>>> GetItemsByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);

        Task<Result<IEnumerable<OrderResponseDto>>> GetOrdersByProductIdAsync(int productId, CancellationToken cancellationToken = default);
    }
}