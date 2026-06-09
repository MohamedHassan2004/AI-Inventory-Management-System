using FluentAssertions;
using Inventory.Application.DTOs.Category;
using Inventory.Application.DTOs.Product;
using Inventory.Application.Interfaces;
using Inventory.Application.Services;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Shared;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Inventory.Application.Test.Service;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<ProductService>> _loggerMock = new();
    private readonly Mock<ILocalizationService> _localizationMock = new();

    public ProductServiceTests()
    {
        _localizationMock
            .Setup(x => x.GetMessage(It.IsAny<string>()))
            .Returns<string>(key => key);
    }

    private ProductService CreateService() =>
        new(
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _localizationMock.Object);

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidProduct_CreatesProductSuccessfully()
    {
        // Arrange
        var dto = new CreateProductDto("SKU-1", "Product 1", 100m, 10, null);
        _productRepositoryMock.Setup(x => x.ExistsBySkuAsync(dto.SKU, null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _productRepositoryMock.Setup(x => x.ExistsByNameAsync(dto.Name, null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _mapperMock.Setup(m => m.Map<ProductResponseDto>(It.IsAny<Product>())).Returns(new ProductResponseDto(1, "SKU-1", "Product 1", 100m, 0m, 10, null));

        var service = CreateService();

        // Act
        var result = await service.CreateAsync(dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _productRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateSku_ReturnsFailure()
    {
        // Arrange
        var dto = new CreateProductDto("SKU-1", "Product 1", 100m, 10, null);
        _productRepositoryMock.Setup(x => x.ExistsBySkuAsync(dto.SKU, null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CreateService();

        // Act
        var result = await service.CreateAsync(dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("DUPLICATE_SKU");
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ReturnsFailure()
    {
        // Arrange
        var dto = new CreateProductDto("SKU-1", "Product 1", 100m, 10, null);
        _productRepositoryMock.Setup(x => x.ExistsBySkuAsync(dto.SKU, null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _productRepositoryMock.Setup(x => x.ExistsByNameAsync(dto.Name, null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CreateService();

        // Act
        var result = await service.CreateAsync(dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("DUPLICATE_NAME");
    }

    [Fact]
    public async Task CreateAsync_CategoryNotFound_ReturnsFailure()
    {
        // Arrange
        var dto = new CreateProductDto("SKU-1", "Product 1", 100m, 10, 99);
        _productRepositoryMock.Setup(x => x.ExistsBySkuAsync(dto.SKU, null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _productRepositoryMock.Setup(x => x.ExistsByNameAsync(dto.Name, null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Category)null!);

        var service = CreateService();

        // Act
        var result = await service.CreateAsync(dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("CATEGORY_NOT_FOUND");
    }

    [Fact]
    public async Task CreateAsync_InvalidData_ReturnsValidationFailure()
    {
        // Arrange
        var dto = new CreateProductDto("SKU-1", "", 100m, 10, null); // Empty name will throw ArgumentException in Product ctor
        _productRepositoryMock.Setup(x => x.ExistsBySkuAsync(dto.SKU, null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _productRepositoryMock.Setup(x => x.ExistsByNameAsync(dto.Name, null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var service = CreateService();

        // Act
        var result = await service.CreateAsync(dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_PRODUCT_DATA");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesSuccessfully()
    {
        // Arrange
        var product = new Product("SKU-1", "Old Name", 100m, 10);
        var dto = new UpdateProductDto("New Name", null);
        
        _productRepositoryMock.Setup(x => x.GetWithBatchesAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _productRepositoryMock.Setup(x => x.ExistsByNameAsync(dto.Name, 1, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _mapperMock.Setup(m => m.Map<ProductResponseDto>(It.IsAny<Product>())).Returns(new ProductResponseDto(1, "SKU-1", "New Name", 100m, 0m, 10, null));

        var service = CreateService();

        // Act
        var result = await service.UpdateAsync(1, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        product.Name.Should().Be("New Name");
        _productRepositoryMock.Verify(x => x.Update(product), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var dto = new UpdateProductDto("New Name", null);
        _productRepositoryMock.Setup(x => x.GetWithBatchesAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Product)null!);

        var service = CreateService();

        // Act
        var result = await service.UpdateAsync(1, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task UpdateAsync_DuplicateName_ReturnsFailure()
    {
        // Arrange
        var product = new Product("SKU-1", "Old Name", 100m, 10);
        var dto = new UpdateProductDto("Duplicate Name", null);
        
        _productRepositoryMock.Setup(x => x.GetWithBatchesAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _productRepositoryMock.Setup(x => x.ExistsByNameAsync(dto.Name, 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CreateService();

        // Act
        var result = await service.UpdateAsync(1, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("DUPLICATE_NAME");
    }

    [Fact]
    public async Task UpdateAsync_CategoryNotFound_ReturnsFailure()
    {
        // Arrange
        var product = new Product("SKU-1", "Old Name", 100m, 10);
        var dto = new UpdateProductDto("New Name", 99);
        
        _productRepositoryMock.Setup(x => x.GetWithBatchesAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _productRepositoryMock.Setup(x => x.ExistsByNameAsync(dto.Name, 1, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Category)null!);

        var service = CreateService();

        // Act
        var result = await service.UpdateAsync(1, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("CATEGORY_NOT_FOUND");
    }

    #endregion

    #region UpdatePriceAsync Tests

    [Fact]
    public async Task UpdatePriceAsync_ValidPrice_UpdatesSuccessfully()
    {
        // Arrange
        var product = new Product("SKU-1", "Product 1", 100m, 10);
        var dto = new UpdateProductPriceDto(150m);
        
        _productRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var service = CreateService();

        // Act
        var result = await service.UpdatePriceAsync(1, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        product.SellingPrice.Should().Be(150m);
        _productRepositoryMock.Verify(x => x.Update(product), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePriceAsync_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var dto = new UpdateProductPriceDto(150m);
        _productRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Product)null!);

        var service = CreateService();

        // Act
        var result = await service.UpdatePriceAsync(1, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task UpdatePriceAsync_InvalidPrice_ReturnsValidationFailure()
    {
        // Arrange
        var product = new Product("SKU-1", "Product 1", 100m, 10);
        var dto = new UpdateProductPriceDto(-10m);
        
        _productRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var service = CreateService();

        // Act
        var result = await service.UpdatePriceAsync(1, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_PRODUCT_DATA");
    }

    #endregion

    #region UpdateReorderPointAsync Tests

    [Fact]
    public async Task UpdateReorderPointAsync_ValidReorderPoint_UpdatesSuccessfully()
    {
        // Arrange
        var product = new Product("SKU-1", "Product 1", 100m, 10);
        var dto = new UpdateProductReorderPointDto(20);
        
        _productRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var service = CreateService();

        // Act
        var result = await service.UpdateReorderPointAsync(1, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        product.ReorderPoint.Should().Be(20);
        _productRepositoryMock.Verify(x => x.Update(product), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateReorderPointAsync_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var dto = new UpdateProductReorderPointDto(20);
        _productRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Product)null!);

        var service = CreateService();

        // Act
        var result = await service.UpdateReorderPointAsync(1, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task UpdateReorderPointAsync_InvalidReorderPoint_ReturnsValidationFailure()
    {
        // Arrange
        var product = new Product("SKU-1", "Product 1", 100m, 10);
        var dto = new UpdateProductReorderPointDto(-5);
        
        _productRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var service = CreateService();

        // Act
        var result = await service.UpdateReorderPointAsync(1, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_PRODUCT_DATA");
    }

    #endregion

    #region Query Methods Tests

    [Fact]
    public async Task GetAllAsync_Always_ReturnsMappedProducts()
    {
        // Arrange
        var products = new List<Product> { new Product("SKU-1", "Product 1", 100m, 10) };
        _productRepositoryMock.Setup(x => x.GetAllWithBatchesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products);
        _mapperMock.Setup(m => m.Map<IEnumerable<ProductResponseDto>>(products)).Returns(new List<ProductResponseDto> { new ProductResponseDto(1, "SKU-1", "Product 1", 100m, 0m, 10, null) });

        var service = CreateService();

        // Act
        var result = await service.GetAllAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetLowStockProductsAsync_ProductsBelowThreshold_ReturnsProducts()
    {
        // Arrange
        var products = new List<Product> { new Product("SKU-1", "Product 1", 100m, 10) }; // has 0 stock
        _productRepositoryMock.Setup(x => x.GetLowStockProductsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products);
        _mapperMock.Setup(m => m.Map<IEnumerable<ProductResponseDto>>(products)).Returns(new List<ProductResponseDto> { new ProductResponseDto(1, "SKU-1", "Product 1", 100m, 0m, 10, null) });

        var service = CreateService();

        // Act
        var result = await service.GetLowStockProductsAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingProduct_ReturnsProduct()
    {
        // Arrange
        var product = new Product("SKU-1", "Product 1", 100m, 10);
        _productRepositoryMock.Setup(x => x.GetWithBatchesAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _mapperMock.Setup(m => m.Map<ProductResponseDto>(product)).Returns(new ProductResponseDto(1, "SKU-1", "Product 1", 100m, 0m, 10, null));

        var service = CreateService();

        // Act
        var result = await service.GetByIdAsync(1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingProduct_ReturnsFailure()
    {
        // Arrange
        _productRepositoryMock.Setup(x => x.GetWithBatchesAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Product)null!);

        var service = CreateService();

        // Act
        var result = await service.GetByIdAsync(1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task SearchAsync_ValidSearchTerm_ReturnsMatchingProducts()
    {
        // Arrange
        var products = new List<Product> { new Product("SKU-1", "Product 1", 100m, 10) };
        _productRepositoryMock.Setup(x => x.SearchAsync("Prod", It.IsAny<CancellationToken>())).ReturnsAsync(products);

        var service = CreateService();

        // Act
        var result = await service.SearchAsync("Prod", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().Name.Should().Be("Product 1");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingProduct_DeletesSuccessfully()
    {
        // Arrange
        var product = new Product("SKU-1", "Product 1", 100m, 10);
        _productRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var service = CreateService();

        // Act
        var result = await service.DeleteAsync(1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _productRepositoryMock.Verify(x => x.Delete(product), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingProduct_ReturnsFailure()
    {
        // Arrange
        _productRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Product)null!);

        var service = CreateService();

        // Act
        var result = await service.DeleteAsync(1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NOT_FOUND");
    }

    #endregion
}
