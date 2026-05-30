using Inventory.Application.DTOs.ReturnOrder;
using Inventory.Application.Interfaces;
using Inventory.Application.Interfaces.Queries;
using Inventory.Domain.Shared;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Inventory.Infrastructure.Queries
{
    public class ReturnOrderQueryService : IReturnOrderQueryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILocalizationService _localizationService;

        public ReturnOrderQueryService(
            ApplicationDbContext context,
            ILocalizationService localizationService)
        {
            _context = context;
            _localizationService = localizationService;
        }

        // ─────────────────────────────────────────────────────────────
        //  Shared EF Core-compatible projection
        // ─────────────────────────────────────────────────────────────
        private static readonly Expression<Func<Domain.Entities.ReturnOrder, ReturnOrderResponseDto>> ToResponseDto =
            r => new ReturnOrderResponseDto
            {
                Id = r.Id,
                OriginalOrderId = r.OriginalOrderId,
                CashierId = r.CashierId,
                CashierName = r.Cashier != null ? r.Cashier.FullName: string.Empty,
                ReturnDate = r.ReturnDate,
                Reason = r.Reason,
                TotalRefundAmount = r.TotalRefundAmount,
                Items = r.Items.Select(i => new ReturnOrderItemResponseDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    RefundAmount = i.Quantity * i.UnitPrice,
                    NewExpiryDate = i.NewExpiryDate
                }).ToList()
            };

        // ─────────────────────────────────────────────────────────────
        //  GET BY ID
        // ─────────────────────────────────────────────────────────────
        public async Task<Result<ReturnOrderResponseDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var dto = await _context.ReturnOrders
                .AsNoTracking()
                .Where(r => r.Id == id)
                .Select(ToResponseDto)
                .FirstOrDefaultAsync(cancellationToken);

            if (dto is null)
                return Result.Failure<ReturnOrderResponseDto>(
                    new Error("NOT_FOUND",
                        _localizationService.GetMessage("ReturnOrderNotFound"),
                        ErrorType.NotFound));

            return Result.Success(dto);
        }

        // ─────────────────────────────────────────────────────────────
        //  GET ALL (filter + pagination — all DB-side)
        // ─────────────────────────────────────────────────────────────
        public async Task<Result<PagedResult<ReturnOrderResponseDto>>> GetAllAsync(
            ReturnOrderFilter filter,
            CancellationToken cancellationToken = default)
        {
            var query = _context.ReturnOrders.AsNoTracking();

            // ── apply filters ──────────────────────────────────────
            if (filter.ProductId.HasValue)
                query = query.Where(r => r.Items.Any(i => i.ProductId == filter.ProductId.Value));

            if (filter.StartDate.HasValue)
                query = query.Where(r => r.ReturnDate >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(r => r.ReturnDate <= filter.EndDate.Value);

            // ── sorting ────────────────────────────────────────────
            query = query.OrderByDescending(r => r.ReturnDate);

            // ── pagination (COUNT + data in 2 DB queries) ──────────
            var totalCount = await query.CountAsync(cancellationToken);
            var skip = (filter.Page - 1) * filter.PageSize;

            var items = await query
                .Skip(skip)
                .Take(filter.PageSize)
                .Select(ToResponseDto)
                .ToListAsync(cancellationToken);

            return Result.Success(new PagedResult<ReturnOrderResponseDto>(items, filter.Page, filter.PageSize, totalCount));
        }
    }
}
