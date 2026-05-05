using Inventory.Application.DTOs.ReturnOrder;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Shared;
using Inventory.Domain.Exceptions;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace Inventory.Application.Services
{
    public class ReturnOrderService : IReturnOrderService
    {
        private readonly IReturnOrderRepository _returnOrderRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ReturnOrderService> _logger;
        private readonly ILocalizationService _localizationService;

        public ReturnOrderService(
            IReturnOrderRepository returnOrderRepository,
            IOrderRepository orderRepository,
            IProductRepository productRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<ReturnOrderService> logger,
            ILocalizationService localizationService)
        {
            _returnOrderRepository = returnOrderRepository;
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _localizationService = localizationService;
        }

        public async Task<Result<ReturnOrderResponseDto>> CreateAsync(
            string cashierId, 
            CreateReturnOrderDto dto, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating return order for original order {OrderId}", dto.OriginalOrderId);

            var itemIds = dto.Items.Select(i => i.OriginalOrderItemId).Distinct().ToList();
            var originalOrder = await _orderRepository.GetOrderForReturnAsync(dto.OriginalOrderId, itemIds, cancellationToken);
            
            if (originalOrder == null)
            {
                return Result.Failure<ReturnOrderResponseDto>(new Error(
                    "ORDER_NOT_FOUND",
                    _localizationService.GetMessage("OrderNotFound"),
                    ErrorType.NotFound));
            }

            ReturnOrder returnOrder;
            try
            {
                returnOrder = new ReturnOrder(originalOrder, cashierId, dto.Reason);

                foreach (var itemDto in dto.Items)
                {
                    var originalItem = originalOrder.Items.FirstOrDefault(i => i.Id == itemDto.OriginalOrderItemId);
                    if (originalItem == null)
                    {
                        return Result.Failure<ReturnOrderResponseDto>(new Error(
                            "ORDER_ITEM_NOT_FOUND",
                            _localizationService.GetMessage("OrderItemNotFound"),
                            ErrorType.NotFound));
                    }

                    // ─────────────────────────────────────────────────────────────
                    // TRACEABILITY: Find original batches and costs via Domain Logic
                    // ─────────────────────────────────────────────────────────────
                    var stockToRestore = originalItem.Return(itemDto.Quantity);

                    // If user didn't provide a new expiry, use the earliest (Min) original expiry of the returned batches as a label
                    var returnItemExpiry = itemDto.NewExpiryDate ?? stockToRestore.Min(x => x.OriginalExpiryDate);
                    returnOrder.AddItem(originalItem, itemDto.Quantity, returnItemExpiry);

                    // Group returned quantities by Supplier, Cost, and Expiry to minimize batch creation
                    // Key: (SupplierId, UnitCost, ExpiryDate)
                    var batchMerger = new Dictionary<(int SupplierId, decimal UnitCost, DateTime ExpiryDate), decimal>();

                    foreach (var info in stockToRestore)
                    {
                        var resolvedExpiry = itemDto.NewExpiryDate ?? info.OriginalExpiryDate;
                        var roundedCost = Math.Round(info.UnitCost, 4);
                        var key = (info.SupplierId, roundedCost, resolvedExpiry);
                        
                        if (batchMerger.ContainsKey(key))
                            batchMerger[key] += info.Quantity;
                        else
                            batchMerger[key] = info.Quantity;
                    }

                    // Perform merged batch additions
                    foreach (var kvp in batchMerger)
                    {
                        originalItem.Product.AddStock(
                            kvp.Key.SupplierId, 
                            kvp.Key.ExpiryDate, 
                            kvp.Key.UnitCost, 
                            kvp.Value);
                    }
                }

                returnOrder.Complete();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Domain rule violation while creating return order");
                return Result.Failure<ReturnOrderResponseDto>(new Error(
                    "INVALID_OPERATION",
                    ex.Message,
                    ErrorType.Validation));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Argument exception while creating return order");
                return Result.Failure<ReturnOrderResponseDto>(new Error(
                    "VALIDATION_ERROR",
                    ex.Message,
                    ErrorType.Validation));
            }
            catch (ReturnQuantityExceededException ex)
            {
                _logger.LogWarning(ex, "Return quantity exceeded");
                return Result.Failure<ReturnOrderResponseDto>(new Error(
                    "RETURN_QUANTITY_EXCEEDED",
                    _localizationService.GetMessage("ReturnQuantityExceeded"),
                    ErrorType.Validation));
            }
            catch (DuplicateReturnItemException ex)
            {
                _logger.LogWarning(ex, "Duplicate return item");
                return Result.Failure<ReturnOrderResponseDto>(new Error(
                    "DUPLICATE_RETURN_ITEM",
                    _localizationService.GetMessage("DuplicateReturnItem"),
                    ErrorType.Validation));
            }

            await _returnOrderRepository.AddAsync(returnOrder, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Return order {ReturnOrderId} created successfully", returnOrder.Id);

            var response = _mapper.Map<ReturnOrderResponseDto>(returnOrder);
            return Result.Success(response);
        }
    }
}
