using Inventory.Application.DTOs.StockBatch;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Shared;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.Services
{
    public class StockBatchService : IStockBatchService
    {
        private readonly IStockBatchRepository _stockBatchRepository;
        private readonly IProductRepository _productRepository;
        private readonly ISupplierRepository _supplierRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<StockBatchService> _logger;
        private readonly ILocalizationService _localizationService;

        public StockBatchService(
            IStockBatchRepository stockBatchRepository,
            IProductRepository productRepository,
            ISupplierRepository supplierRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<StockBatchService> logger,
            ILocalizationService localizationService)
        {
            _stockBatchRepository = stockBatchRepository;
            _productRepository = productRepository;
            _supplierRepository = supplierRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _localizationService = localizationService;
        }

        public async Task<Result<StockBatchResponseDto>> CreateAsync(CreateStockBatchDto dto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating new StockBatch for ProductId: {ProductId}", dto.ProductId);

            var product = await _productRepository.GetByIdAsync(dto.ProductId, cancellationToken);
            if (product == null)
            {
                _logger.LogWarning("Product not found with Id: {ProductId}", dto.ProductId);
                return Result.Failure<StockBatchResponseDto>(new Error(
                    "PRODUCT_NOT_FOUND",
                    _localizationService.GetMessage("ProductNotFound") ?? "Product not found.",
                    ErrorType.NotFound));
            }

            var supplier = await _supplierRepository.GetByIdAsync(dto.SupplierId, cancellationToken);
            if (supplier == null)
            {
                _logger.LogWarning("Supplier not found with Id: {SupplierId}", dto.SupplierId);
                return Result.Failure<StockBatchResponseDto>(new Error(
                    "SUPPLIER_NOT_FOUND",
                    _localizationService.GetMessage("SupplierNotFound") ?? "Supplier not found.",
                    ErrorType.NotFound));
            }

            StockBatch batch;
            try
            {
                batch = new StockBatch(dto.ProductId, dto.SupplierId, dto.PurchaseDate, dto.ExpireDate, dto.UnitCost, dto.OriginalQuantity);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid StockBatch data provided.");
                return Result.Failure<StockBatchResponseDto>(new Error(
                    "INVALID_BATCH_DATA",
                    _localizationService.GetMessage("InvalidBatchData") ?? "Invalid batch data.",
                    ErrorType.Validation));
            }

            await _stockBatchRepository.AddAsync(batch, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = _mapper.Map<StockBatchResponseDto>(batch);
            return Result.Success(response);
        }

        public async Task<Result<StockBatchResponseDto>> UpdateAsync(int id, UpdateStockBatchDto dto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating StockBatch with Id: {Id}", id);

            var batch = await _stockBatchRepository.GetByIdAsync(id, cancellationToken);
            if (batch == null)
            {
                _logger.LogWarning("StockBatch not found with Id: {Id}", id);
                return Result.Failure<StockBatchResponseDto>(new Error(
                    "NOT_FOUND",
                    _localizationService.GetMessage("BatchNotFound") ?? "Batch not found.",
                    ErrorType.NotFound));
            }

            batch.UpdateBatch(dto.ExpireDate, dto.UnitCost, dto.RemainingQuantity);

            _stockBatchRepository.Update(batch);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = _mapper.Map<StockBatchResponseDto>(batch);
            return Result.Success(response);
        }

        public async Task<Result<IEnumerable<StockBatchResponseDto>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching all StockBatches");
            var batches = await _stockBatchRepository.GetAllWithDetailsAsync(cancellationToken);
            var response = _mapper.Map<IEnumerable<StockBatchResponseDto>>(batches);
            return Result.Success(response);
        }

        public async Task<Result<StockBatchResponseDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching StockBatch with Id: {Id}", id);
            var batch = await _stockBatchRepository.GetWithDetailsAsync(id, cancellationToken);
            if (batch == null)
            {
                _logger.LogWarning("StockBatch not found with Id: {Id}", id);
                return Result.Failure<StockBatchResponseDto>(new Error(
                    "NOT_FOUND",
                    _localizationService.GetMessage("BatchNotFound") ?? "Batch not found.",
                    ErrorType.NotFound));
            }
            var response = _mapper.Map<StockBatchResponseDto>(batch);
            return Result.Success(response);
        }

        public async Task<Result<IEnumerable<StockBatchResponseDto>>> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching StockBatches for Product Id: {ProductId}", productId);
            var batches = await _stockBatchRepository.GetByProductIdAsync(productId, cancellationToken);
            var response = _mapper.Map<IEnumerable<StockBatchResponseDto>>(batches);
            return Result.Success(response);
        }

        public async Task<Result<IEnumerable<StockBatchResponseDto>>> GetExpiringBatchesAsync(int daysUntilExpiry, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching StockBatches expiring within {Days} days", daysUntilExpiry);
            var thresholdDate = DateTime.UtcNow.AddDays(daysUntilExpiry);
            var batches = await _stockBatchRepository.GetExpiringBatchesAsync(thresholdDate, cancellationToken);
            var response = _mapper.Map<IEnumerable<StockBatchResponseDto>>(batches);
            return Result.Success(response);
        }

        public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deleting StockBatch with Id: {Id}", id);

            var batch = await _stockBatchRepository.GetByIdAsync(id, cancellationToken);
            if (batch == null)
            {
                _logger.LogWarning("StockBatch not found with Id: {Id}", id);
                return Result.Failure(new Error(
                    "NOT_FOUND",
                    _localizationService.GetMessage("BatchNotFound") ?? "Batch not found.",
                    ErrorType.NotFound));
            }

            _stockBatchRepository.Delete(batch);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("StockBatch deleted successfully with Id: {Id}", id);
            return Result.Success();
        }
    }
}
