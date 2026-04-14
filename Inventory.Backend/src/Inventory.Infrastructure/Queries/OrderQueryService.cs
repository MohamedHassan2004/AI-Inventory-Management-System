using Inventory.Application.DTOs.Order;
using Inventory.Application.Interfaces;
using Inventory.Domain.Enums;
using Inventory.Domain.Shared;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Queries
{
    public class OrderQueryService : IOrderQueryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILocalizationService _localizationService;

        public OrderQueryService(
            ApplicationDbContext context,
            ILocalizationService localizationService)
        {
            _context = context;
            _localizationService = localizationService;
        }

        public async Task<Result<OrderResponseDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Select(o => new OrderResponseDto
                {
                    Id = o.Id,
                    FinalTotal = o.FinalTotal,
                    Items = o.Items.Select(i => new OrderItemResponseDto
                    {
                        Id = i.Id,
                        ProductName = i.Product.Name,
                        Quantity = i.Quantity
                    }).ToList()
                })
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken); // 🔥 optimized

            if (order == null)
                return Result.Failure<OrderResponseDto>(
                    new Error(
                        "NOT_FOUND",
                        _localizationService.GetMessage("OrderNotFound"),
                        ErrorType.NotFound));

            return Result.Success(order);
        }

        public async Task<Result<IEnumerable<OrderResponseDto>>> GetPendingAsync(CancellationToken cancellationToken = default)
        {
            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.Status == OrderStatus.Pending)
                .Select(o => new OrderResponseDto
                {
                    Id = o.Id,
                    FinalTotal = o.FinalTotal
                })
                .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<OrderResponseDto>>(orders);
        }

        public async Task<Result<IEnumerable<OrderItemResponseDto>>> GetItemsByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
        {
            var exists = await _context.Orders
                .AsNoTracking()
                .AnyAsync(o => o.Id == orderId, cancellationToken);

            if (!exists)
                return Result.Failure<IEnumerable<OrderItemResponseDto>>(
                    new Error(
                        "NOT_FOUND",
                        _localizationService.GetMessage("OrderNotFound"),
                        ErrorType.NotFound));

            var items = await _context.OrderItems
                .AsNoTracking()
                .Where(i => i.OrderId == orderId)
                .Select(i => new OrderItemResponseDto
                {
                    Id = i.Id,
                    ProductName = i.Product.Name,
                    Quantity = i.Quantity
                })
                .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<OrderItemResponseDto>>(items);
        }

        public async Task<Result<IEnumerable<OrderResponseDto>>> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default)
        {
            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.Items.Any(i => i.ProductId == productId))
                .Select(o => new OrderResponseDto
                {
                    Id = o.Id,
                    FinalTotal = o.FinalTotal
                })
                .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<OrderResponseDto>>(orders);
        }
    }
}