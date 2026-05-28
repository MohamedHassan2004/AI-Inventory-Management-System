using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.DTOs.PurchaseOrder;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Shared;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace Inventory.Application.Services
{
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly ISupplierRepository _supplierRepository;
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<PurchaseOrderService> _logger;
        private readonly ILocalizationService _localizationService;

        public PurchaseOrderService(
            IPurchaseOrderRepository purchaseOrderRepository,
            ISupplierRepository supplierRepository,
            IProductRepository productRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<PurchaseOrderService> logger,
            ILocalizationService localizationService)
        {
            _purchaseOrderRepository = purchaseOrderRepository;
            _supplierRepository = supplierRepository;
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _localizationService = localizationService;
        }

        // ─────────────────────────────────────────────────────────────
        //  Public entry point — thin orchestrator
        // ─────────────────────────────────────────────────────────────

        public async Task<Result<PurchaseOrderResponseDto>> SubmitAsync(
            SubmitPurchaseOrderDto dto,
            CancellationToken cancellationToken = default)
        {
            if (dto.Items == null || !dto.Items.Any())
            {
                _logger.LogWarning("Attempted to submit an empty purchase order for supplier {SupplierId}", dto.SupplierId);
                return Result.Failure<PurchaseOrderResponseDto>(new Error(
                    "EMPTY_PURCHASE_ORDER",
                    _localizationService.GetMessage("EmptyPurchaseOrder"),
                    ErrorType.Validation));
            }

            _logger.LogInformation(
                "Submitting purchase order for supplier {SupplierId} with {ItemCount} items",
                dto.SupplierId, dto.Items.Count);

            var supplierResult = await ValidateSupplierAsync(dto.SupplierId, cancellationToken);
            if (!supplierResult.IsSuccess)
                return Result.Failure<PurchaseOrderResponseDto>(supplierResult.Error);

            var productsResult = await ValidateProductsAsync(dto.Items, cancellationToken);
            if (!productsResult.IsSuccess)
                return Result.Failure<PurchaseOrderResponseDto>(productsResult.Error);

            var purchaseOrder = BuildPurchaseOrder(dto, productsResult.Value);

            await _purchaseOrderRepository.AddAsync(purchaseOrder, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Purchase Order {OrderId} submitted successfully", purchaseOrder.Id);

            var responseDto = _mapper.Map<PurchaseOrderResponseDto>(purchaseOrder);
            responseDto.SupplierName = supplierResult.Value.Name;

            return Result.Success(responseDto);
        }

        // ─────────────────────────────────────────────────────────────
        //  Private helpers
        // ─────────────────────────────────────────────────────────────

        private async Task<Result<Supplier>> ValidateSupplierAsync(
            int supplierId,
            CancellationToken cancellationToken)
        {
            var supplier = await _supplierRepository.GetByIdAsync(supplierId, cancellationToken);
            if (supplier is null)
            {
                _logger.LogWarning("Supplier not found: {SupplierId}", supplierId);
                return Result.Failure<Supplier>(new Error(
                    "SUPPLIER_NOT_FOUND",
                    _localizationService.GetMessage("SupplierNotFound"),
                    ErrorType.NotFound));
            }

            return Result.Success(supplier);
        }

        private async Task<Result<Dictionary<int, Product>>> ValidateProductsAsync(
            List<PurchaseOrderItemDto> items,
            CancellationToken cancellationToken)
        {
            var productIds = items.Select(i => i.ProductId).Distinct().ToList();
            var products = (await _productRepository.GetWithBatchesListAsync(productIds, cancellationToken)).ToList();

            var missingIds = productIds.Except(products.Select(p => p.Id)).ToList();
            if (missingIds.Any())
            {
                _logger.LogWarning("Products not found: {MissingIds}", string.Join(", ", missingIds));
                return Result.Failure<Dictionary<int, Product>>(new Error(
                    "PRODUCT_NOT_FOUND",
                    _localizationService.GetMessage("ProductNotFound"),
                    ErrorType.NotFound));
            }

            return Result.Success(products.ToDictionary(p => p.Id));
        }

        private static PurchaseOrder BuildPurchaseOrder(
            SubmitPurchaseOrderDto dto,
            Dictionary<int, Product> productMap)
        {
            var order = PurchaseOrder.Create(dto.SupplierId);

            foreach (var item in dto.Items)
                order.AddItem(productMap[item.ProductId], item.Quantity, item.UnitCost, item.ExpiryDate, item.DiscountPercentage);

            order.Complete();

            return order;
        }
    }
}
