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
                    OrderDate = o.OrderDate,
                    CashierId = o.CashierId,
                    Status = o.Status,
                    Type = o.Type,
                    PaymentMethod = o.PaymentMethod,
                    SubTotal = o.SubTotal,
                    DiscountPercentage = o.DiscountPercentage,
                    DiscountAmount = o.DiscountAmount,
                    TaxAmount = o.TaxAmount,
                    FinalTotal = o.FinalTotal,
                    Items = o.Items.Select(i => new OrderItemResponseDto
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ProductName = i.Product.Name,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        TotalPrice = i.Quantity * i.UnitPrice
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
                    OrderDate = o.OrderDate,
                    CashierId = o.CashierId,
                    Status = o.Status,
                    Type = o.Type,
                    PaymentMethod = o.PaymentMethod,
                    SubTotal = o.SubTotal,
                    DiscountPercentage = o.DiscountPercentage,
                    DiscountAmount = o.DiscountAmount,
                    TaxAmount = o.TaxAmount,
                    FinalTotal = o.FinalTotal
                })
                .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<OrderResponseDto>>(orders);
        }

        public async Task<Result<IEnumerable<OrderItemResponseDto>>> GetItemsByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Where(o => o.Id == orderId)
                .Select(o => new 
                {
                    Items = o.Items.Select(i => new OrderItemResponseDto
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ProductName = i.Product.Name,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        TotalPrice = i.Quantity * i.UnitPrice
                    }).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (order == null)
                return Result.Failure<IEnumerable<OrderItemResponseDto>>(
                    new Error(
                        "NOT_FOUND",
                        _localizationService.GetMessage("OrderNotFound"),
                        ErrorType.NotFound));

            return Result.Success<IEnumerable<OrderItemResponseDto>>(order.Items);
        }

        public async Task<Result<IEnumerable<OrderResponseDto>>> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default)
        {
            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.Items.Any(i => i.ProductId == productId))
                .Select(o => new OrderResponseDto
                {
                    Id = o.Id,
                    OrderDate = o.OrderDate,
                    CashierId = o.CashierId,
                    Status = o.Status,
                    Type = o.Type,
                    PaymentMethod = o.PaymentMethod,
                    SubTotal = o.SubTotal,
                    DiscountPercentage = o.DiscountPercentage,
                    DiscountAmount = o.DiscountAmount,
                    TaxAmount = o.TaxAmount,
                    FinalTotal = o.FinalTotal
                })
                .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<OrderResponseDto>>(orders);
        }
    }
}