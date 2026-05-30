using Inventory.Application.DTOs.Order;
using Inventory.Application.Interfaces;
using Inventory.Domain.Enums;
using Inventory.Domain.Entities;
using Inventory.Domain.Shared;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Inventory.Application.Interfaces.Queries;

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

        // ─────────────────────────────────────────────────────────────
        //  Shared EF Core-compatible projection
        // ─────────────────────────────────────────────────────────────
        private static readonly Expression<Func<Order, DetailedOrderResponseDto>> ToDetailedOrderResponseDto = o => new DetailedOrderResponseDto
        {
            Id = o.Id,
            OrderDate = o.OrderDate,
            CashierId = o.CashierId,
            CashierName = o.Cashier != null ? o.Cashier.FullName : string.Empty,
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
                ProductName = i.Product != null ? i.Product.Name : string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.Allocations.Any()
                    ? i.Allocations.Sum(a => a.QuantityTaken * a.UnitPrice * (1 - a.DiscountPercentage / 100m))
                    : i.Quantity * i.UnitPrice,
                Allocations = i.Allocations.Select(a => new OrderItemBatchAllocationResponseDto
                {
                    StockBatchId = a.StockBatchId,
                    Quantity = a.QuantityTaken,
                    UnitPrice = a.UnitPrice,
                    DiscountPercentage = a.DiscountPercentage
                }).ToList()
            }).ToList(),
            RowVersion = Convert.ToBase64String(o.RowVersion)
        };

        private static readonly Expression<Func<Order, OrderResponseDto>> ToOrderResponseDto = o => new OrderResponseDto
        {
            Id = o.Id,
            OrderDate = o.OrderDate,
            CashierId = o.CashierId,
            CashierName = o.Cashier != null ? o.Cashier.FullName : string.Empty,
            Status = o.Status,
            Type = o.Type,
            PaymentMethod = o.PaymentMethod,
            FinalTotal = o.FinalTotal
        };

        // ─────────────────────────────────────────────────────────────
        //  GET BY ID
        // ─────────────────────────────────────────────────────────────
        public async Task<Result<DetailedOrderResponseDto>> GetByIdAsync(
            string cashierId,
            int id,
            CancellationToken cancellationToken = default)
        {
            var accessInfo = await _context.Orders
                .AsNoTracking()
                .Where(o => o.Id == id)
                .Select(o => new { o.Status, o.CashierId })
                .FirstOrDefaultAsync(cancellationToken);

            if (accessInfo is null)
                return Result.Failure<DetailedOrderResponseDto>(OrderNotFoundError());

            if (accessInfo.Status == OrderStatus.Draft && accessInfo.CashierId != cashierId)
                return Result.Failure<DetailedOrderResponseDto>(DraftOrderAccessDeniedError());

            var order = await _context.Orders
                .AsNoTracking()
                .AsSplitQuery()
                .Where(o => o.Id == id)
                .Select(ToDetailedOrderResponseDto)
                .FirstOrDefaultAsync(cancellationToken);

            if (order is null)
                return Result.Failure<DetailedOrderResponseDto>(OrderNotFoundError());

            return Result.Success(order);
        }

        // ─────────────────────────────────────────────────────────────
        //  GET ALL (with filter + pagination)
        // ─────────────────────────────────────────────────────────────
        public async Task<Result<PagedResult<OrderResponseDto>>> GetAllAsync(
            OrderFilter filter,
            CancellationToken cancellationToken = default)
        {
            filter.Page = Math.Max(1, filter.Page);
            filter.PageSize = filter.PageSize <= 0 ? 20 : filter.PageSize;
            
            var query = _context.Orders.AsNoTracking();

            // ── apply filters ──────────────────────────────────────
            if (filter.Status.HasValue)
                query = query.Where(o => o.Status == filter.Status.Value);

            if (filter.Type.HasValue)
                query = query.Where(o => o.Type == filter.Type.Value);

            if (filter.PaymentMethod.HasValue)
                query = query.Where(o => o.PaymentMethod == filter.PaymentMethod.Value);

            if (filter.ProductId.HasValue)
                query = query.Where(o => o.Items.Any(i => i.ProductId == filter.ProductId.Value));

            if (!string.IsNullOrWhiteSpace(filter.CashierId))
                query = query.Where(o => o.CashierId == filter.CashierId);

            if (filter.DateFrom.HasValue)
                query = query.Where(o => o.OrderDate >= filter.DateFrom.Value);

            if (filter.DateTo.HasValue)
                query = query.Where(o => o.OrderDate <= filter.DateTo.Value);

            if (filter.MinTotal.HasValue)
                query = query.Where(o => o.FinalTotal >= filter.MinTotal.Value);

            if (filter.MaxTotal.HasValue)
                query = query.Where(o => o.FinalTotal <= filter.MaxTotal.Value);

            // ── sorting ────────────────────────────────────────────
            query = (filter.SortBy, filter.SortDescending) switch
            {
                (OrderSortBy.FinalTotal,  true)  => query.OrderByDescending(o => o.FinalTotal),
                (OrderSortBy.FinalTotal,  false) => query.OrderBy(o => o.FinalTotal),
                (_,                       true)  => query.OrderByDescending(o => o.OrderDate),  // default
                (_,                       false) => query.OrderBy(o => o.OrderDate),
            };

            // ── pagination ─────────────────────────────────────────
            var totalCount = await query.CountAsync(cancellationToken);
            var skip = (filter.Page - 1) * filter.PageSize;

            var items = await query
                .Skip(skip)
                .Take(filter.PageSize)
                .Select(ToOrderResponseDto)
                .ToListAsync(cancellationToken);

            return Result.Success(new PagedResult<OrderResponseDto>(items, filter.Page, filter.PageSize, totalCount));
        }

        // ─────────────────────────────────────────────────────────────
        //  GET ITEMS BY ORDER ID
        // ─────────────────────────────────────────────────────────────
        public async Task<Result<IEnumerable<OrderItemResponseDto>>> GetItemsByOrderIdAsync(
            string cashierId,
            int orderId,
            CancellationToken cancellationToken = default)
        {
            var accessInfo = await _context.Orders
                .AsNoTracking()
                .Where(o => o.Id == orderId)
                .Select(o => new { o.Status, o.CashierId })
                .FirstOrDefaultAsync(cancellationToken);

            if (accessInfo is null)
                return Result.Failure<IEnumerable<OrderItemResponseDto>>(OrderNotFoundError());

            if (accessInfo.Status == OrderStatus.Draft && accessInfo.CashierId != cashierId)
                return Result.Failure<IEnumerable<OrderItemResponseDto>>(DraftOrderAccessDeniedError());

            var order = await _context.Orders
                .AsNoTracking()
                .AsSplitQuery()
                .Where(o => o.Id == orderId)
                .Select(o => new
                {
                    Items = o.Items.Select(i => new OrderItemResponseDto
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ProductName = i.Product != null ? i.Product.Name : string.Empty,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        TotalPrice = i.Allocations.Any()
                            ? i.Allocations.Sum(a => a.QuantityTaken * a.UnitPrice * (1 - a.DiscountPercentage / 100m))
                            : i.Quantity * i.UnitPrice,
                        Allocations = i.Allocations.Select(a => new OrderItemBatchAllocationResponseDto
                        {
                            StockBatchId = a.StockBatchId,
                            Quantity = a.QuantityTaken,
                            UnitPrice = a.UnitPrice,
                            DiscountPercentage = a.DiscountPercentage
                        }).ToList()
                    }).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (order is null)
                return Result.Failure<IEnumerable<OrderItemResponseDto>>(OrderNotFoundError());

            return Result.Success<IEnumerable<OrderItemResponseDto>>(order.Items);
        }

        private Error OrderNotFoundError() =>
            new("NOT_FOUND", _localizationService.GetMessage("OrderNotFound"), ErrorType.NotFound);

        private Error DraftOrderAccessDeniedError() =>
            new("DRAFT_ORDER_ACCESS_DENIED",
                _localizationService.GetMessage("DraftOrderAccessDenied"),
                ErrorType.Forbidden);
    }
}
