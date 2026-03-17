using Inventory.Application.DTOs.Product;
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
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductService> _logger;
        private readonly ILocalizationService _localizationService;

        public ProductService(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<ProductService> logger,
            ILocalizationService localizationService)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _localizationService = localizationService;
        }

        public async Task<Result<ProductResponseDto>> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating product with SKU: {SKU}", dto.SKU);

            if (await _productRepository.ExistsBySkuAsync(dto.SKU, null, cancellationToken))
            {
                _logger.LogWarning("Duplicate product SKU detected: {SKU}", dto.SKU);
                return Result.Failure<ProductResponseDto>(new Error(
                    "DUPLICATE_SKU",
                    _localizationService.GetMessage("ProductDuplicateSku") ?? "Duplicate SKU.",
                    ErrorType.Conflict));
            }
            
            if (await _productRepository.ExistsByNameAsync(dto.Name, null, cancellationToken))
            {
                _logger.LogWarning("Duplicate product name detected: {Name}", dto.Name);
                return Result.Failure<ProductResponseDto>(new Error(
                    "DUPLICATE_NAME",
                    _localizationService.GetMessage("ProductDuplicateName") ?? "Duplicate Name.",
                    ErrorType.Conflict));
            }

            if (dto.CategoryId.HasValue)
            {
                var category = await _categoryRepository.GetByIdAsync(dto.CategoryId.Value, cancellationToken);
                if (category == null)
                {
                    _logger.LogWarning("Category not found with Id: {CategoryId}", dto.CategoryId.Value);
                    return Result.Failure<ProductResponseDto>(new Error(
                        "CATEGORY_NOT_FOUND",
                        _localizationService.GetMessage("CategoryNotFound") ?? "Category not found.",
                        ErrorType.NotFound));
                }
            }

            Product product;
            try
            {
                product = new Product(dto.SKU, dto.Name, dto.SellingPrice, dto.ReorderPoint)
                {
                    CategoryId = dto.CategoryId
                };
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid product data provided");
                return Result.Failure<ProductResponseDto>(new Error(
                    "INVALID_PRODUCT_DATA",
                    _localizationService.GetMessage("InvalidProductData") ?? "Invalid product data.",
                    ErrorType.Validation));
            }

            await _productRepository.AddAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = _mapper.Map<ProductResponseDto>(product);
            return Result.Success(response);
        }

        public async Task<Result<ProductResponseDto>> UpdateAsync(int id, UpdateProductDto dto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating product with Id: {Id}", id);

            var product = await _productRepository.GetWithBatchesAsync(id, cancellationToken);
            if (product == null)
            {
                _logger.LogWarning("Product not found with Id: {Id}", id);
                return Result.Failure<ProductResponseDto>(new Error(
                    "NOT_FOUND",
                    _localizationService.GetMessage("ProductNotFound") ?? "Product not found.",
                    ErrorType.NotFound));
            }

            if (await _productRepository.ExistsBySkuAsync(dto.SKU, id, cancellationToken))
            {
                _logger.LogWarning("Duplicate product SKU detected: {SKU}", dto.SKU);
                return Result.Failure<ProductResponseDto>(new Error(
                    "DUPLICATE_SKU",
                    _localizationService.GetMessage("ProductDuplicateSku") ?? "Duplicate SKU.",
                    ErrorType.Conflict));
            }

            if (await _productRepository.ExistsByNameAsync(dto.Name, id, cancellationToken))
            {
                _logger.LogWarning("Duplicate product name detected: {Name}", dto.Name);
                return Result.Failure<ProductResponseDto>(new Error(
                    "DUPLICATE_NAME",
                    _localizationService.GetMessage("ProductDuplicateName") ?? "Duplicate Name.",
                    ErrorType.Conflict));
            }

            if (dto.CategoryId.HasValue)
            {
                var category = await _categoryRepository.GetByIdAsync(dto.CategoryId.Value, cancellationToken);
                if (category == null)
                {
                    _logger.LogWarning("Category not found with Id: {CategoryId}", dto.CategoryId.Value);
                    return Result.Failure<ProductResponseDto>(new Error(
                        "CATEGORY_NOT_FOUND",
                        _localizationService.GetMessage("CategoryNotFound") ?? "Category not found.",
                        ErrorType.NotFound));
                }
            }

            product.SKU = dto.SKU;
            product.Name = dto.Name;
            product.CategoryId = dto.CategoryId;

            _productRepository.Update(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Re-fetch category for DTO mapping if it was changed
            if (dto.CategoryId.HasValue && product.Category == null)
            {
                product.Category = await _categoryRepository.GetByIdAsync(dto.CategoryId.Value, cancellationToken);
            }

            var response = _mapper.Map<ProductResponseDto>(product);
            return Result.Success(response);
        }

        public async Task<Result> UpdatePriceAsync(int id, UpdateProductPriceDto dto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating product price for Id: {Id}", id);

            var product = await _productRepository.GetByIdAsync(id, cancellationToken);
            if (product == null)
            {
                _logger.LogWarning("Product not found with Id: {Id}", id);
                return Result.Failure(new Error(
                    "NOT_FOUND",
                    _localizationService.GetMessage("ProductNotFound") ?? "Product not found.",
                    ErrorType.NotFound));
            }

            try
            {
                product.UpdatePrice(dto.SellingPrice);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid product price provided");
                return Result.Failure(new Error(
                    "INVALID_PRODUCT_DATA",
                    _localizationService.GetMessage("InvalidProductData") ?? "Invalid product data.",
                    ErrorType.Validation));
            }

            _productRepository.Update(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        public async Task<Result> UpdateReorderPointAsync(int id, UpdateProductReorderPointDto dto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating product reorder point for Id: {Id}", id);

            var product = await _productRepository.GetByIdAsync(id, cancellationToken);
            if (product == null)
            {
                _logger.LogWarning("Product not found with Id: {Id}", id);
                return Result.Failure(new Error(
                    "NOT_FOUND",
                    _localizationService.GetMessage("ProductNotFound") ?? "Product not found.",
                    ErrorType.NotFound));
            }

            try
            {
                product.UpdateReorderPoint(dto.ReorderPoint);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid product reorder point provided");
                return Result.Failure(new Error(
                    "INVALID_PRODUCT_DATA",
                    _localizationService.GetMessage("InvalidProductData") ?? "Invalid product data.",
                    ErrorType.Validation));
            }

            _productRepository.Update(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        public async Task<Result<IEnumerable<ProductResponseDto>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching all products");
            var products = await _productRepository.GetAllWithBatchesAsync(cancellationToken);
            var response = _mapper.Map<IEnumerable<ProductResponseDto>>(products);
            return Result.Success(response);
        }

        public async Task<Result<IEnumerable<ProductResponseDto>>> GetLowStockProductsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching low stock products");
            var products = await _productRepository.GetLowStockProductsAsync(cancellationToken);
            var response = _mapper.Map<IEnumerable<ProductResponseDto>>(products);
            return Result.Success(response);
        }

        public async Task<Result<ProductResponseDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching product with Id: {Id}", id);
            var product = await _productRepository.GetWithBatchesAsync(id, cancellationToken);
            if (product == null)
            {
                _logger.LogWarning("Product not found with Id: {Id}", id);
                return Result.Failure<ProductResponseDto>(new Error(
                    "NOT_FOUND",
                    _localizationService.GetMessage("ProductNotFound") ?? "Product not found.",
                    ErrorType.NotFound));
            }
            var response = _mapper.Map<ProductResponseDto>(product);
            return Result.Success(response);
        }

        public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deleting product with Id: {Id}", id);

            var product = await _productRepository.GetByIdAsync(id, cancellationToken);
            if (product == null)
            {
                _logger.LogWarning("Product not found with Id: {Id}", id);
                return Result.Failure(new Error(
                    "NOT_FOUND",
                    _localizationService.GetMessage("ProductNotFound") ?? "Product not found.",
                    ErrorType.NotFound));
            }

            // In future maybe soft delete, but using generic Delete for now as Product has no IsDeleted
            _productRepository.Delete(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Product deleted successfully with Id: {Id}", id);
            return Result.Success();
        }
    }
}
