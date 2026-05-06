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

            var originalOrder = await _orderRepository.GetFullOrderAsync(dto.OriginalOrderId, cancellationToken);
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

                    returnOrder.AddItem(originalItem, itemDto.Quantity, itemDto.NewExpiryDate);

                    // Restore stock into the original batch — preserves the correct SupplierId and UnitCost
                    originalItem.Product.AddReturnedStock(itemDto.NewExpiryDate, itemDto.Quantity);
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
