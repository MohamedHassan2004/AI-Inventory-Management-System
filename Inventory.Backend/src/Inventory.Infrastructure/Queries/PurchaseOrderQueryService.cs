using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.DTOs.PurchaseOrder;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Shared;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Queries
{
    public class PurchaseOrderQueryService : IPurchaseOrderQueryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILocalizationService _localizationService;

        public PurchaseOrderQueryService(
            ApplicationDbContext context,
            ILocalizationService localizationService)
        {
            _context = context;
            _localizationService = localizationService;
        }

        private static readonly Expression<Func<PurchaseOrder, PurchaseOrderResponseDto>> ToResponseDto = o => new PurchaseOrderResponseDto
        {
            Id = o.Id,
            OrderDate = o.OrderDate,
            SupplierId = o.SupplierId,
            SupplierName = o.Supplier.Name,
            Status = o.Status,
            FinalTotal = o.FinalTotal,
            Items = o.Items.Select(i => new PurchaseOrderItemResponseDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost,
                ExpiryDate = i.ExpiryDate,
                TotalPrice = i.Quantity * i.UnitCost
            }).ToList()
        };

        public async Task<Result<PurchaseOrderResponseDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var order = await _context.PurchaseOrders
                .AsNoTracking()
                .Where(o => o.Id == id)
                .Select(ToResponseDto)
                .FirstOrDefaultAsync(cancellationToken);

            if (order is null)
                return Result.Failure<PurchaseOrderResponseDto>(
                    new Error("NOT_FOUND",
                        _localizationService.GetMessage("PurchaseOrderNotFound"),
                        ErrorType.NotFound));

            return Result.Success(order);
        }

        public async Task<Result<PagedResult<PurchaseOrderResponseDto>>> GetAllAsync(
            PurchaseOrderFilter filter,
            CancellationToken cancellationToken = default)
        {
            filter.Page = Math.Max(1, filter.Page);
            filter.PageSize = filter.PageSize <= 0 ? 20 : filter.PageSize;

            var query = _context.PurchaseOrders.AsNoTracking();

            if (filter.Status.HasValue)
                query = query.Where(o => o.Status == filter.Status.Value);

            if (filter.SupplierId.HasValue)
                query = query.Where(o => o.SupplierId == filter.SupplierId.Value);

            if (filter.ProductId.HasValue)
                query = query.Where(o => o.Items.Any(i => i.ProductId == filter.ProductId.Value));

            if (filter.DateFrom.HasValue)
                query = query.Where(o => o.OrderDate >= filter.DateFrom.Value);

            if (filter.DateTo.HasValue)
                query = query.Where(o => o.OrderDate <= filter.DateTo.Value);

            if (filter.MinTotal.HasValue)
                query = query.Where(o => o.FinalTotal >= filter.MinTotal.Value);

            if (filter.MaxTotal.HasValue)
                query = query.Where(o => o.FinalTotal <= filter.MaxTotal.Value);

            query = (filter.SortBy, filter.SortDescending) switch
            {
                (PurchaseOrderSortBy.FinalTotal, true) => query.OrderByDescending(o => o.FinalTotal),
                (PurchaseOrderSortBy.FinalTotal, false) => query.OrderBy(o => o.FinalTotal),
                (PurchaseOrderSortBy.Status, true) => query.OrderByDescending(o => o.Status),
                (PurchaseOrderSortBy.Status, false) => query.OrderBy(o => o.Status),
                (_, true) => query.OrderByDescending(o => o.OrderDate),
                (_, false) => query.OrderBy(o => o.OrderDate),
            };

            var totalCount = await query.CountAsync(cancellationToken);
            var skip = (filter.Page - 1) * filter.PageSize;

            var items = await query
                .Skip(skip)
                .Take(filter.PageSize)
                .Select(ToResponseDto)
                .ToListAsync(cancellationToken);

            return Result.Success(new PagedResult<PurchaseOrderResponseDto>(items, filter.Page, filter.PageSize, totalCount));
        }

        public async Task<Result<IEnumerable<PurchaseOrderItemResponseDto>>> GetItemsByPurchaseOrderIdAsync(
            int purchaseOrderId,
            CancellationToken cancellationToken = default)
        {
            var order = await _context.PurchaseOrders
                .AsNoTracking()
                .Where(o => o.Id == purchaseOrderId)
                .Select(o => new
                {
                    Items = o.Items.Select(i => new PurchaseOrderItemResponseDto
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ProductName = i.Product.Name,
                        Quantity = i.Quantity,
                        UnitCost = i.UnitCost,
                        ExpiryDate = i.ExpiryDate,
                        TotalPrice = i.Quantity * i.UnitCost
                    }).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (order is null)
                return Result.Failure<IEnumerable<PurchaseOrderItemResponseDto>>(
                    new Error("NOT_FOUND",
                        _localizationService.GetMessage("PurchaseOrderNotFound"),
                        ErrorType.NotFound));

            return Result.Success<IEnumerable<PurchaseOrderItemResponseDto>>(order.Items);
        }
    }
}
