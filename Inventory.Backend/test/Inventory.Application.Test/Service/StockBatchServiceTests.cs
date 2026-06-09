using FluentAssertions;
using Inventory.Application.DTOs.StockBatch;
using Inventory.Application.Interfaces;
using Inventory.Application.Services;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Shared;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Inventory.Application.Test.Service;

public class StockBatchServiceTests
{
    private readonly Mock<IStockBatchRepository> _stockBatchRepositoryMock = new();
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<ISupplierRepository> _supplierRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<StockBatchService>> _loggerMock = new();
    private readonly Mock<ILocalizationService> _localizationMock = new();

    public StockBatchServiceTests()
    {
        _localizationMock
            .Setup(x => x.GetMessage(It.IsAny<string>()))
            .Returns<string>(key => key);
    }

    private StockBatchService CreateService() =>
        new(
            _stockBatchRepositoryMock.Object,
            _productRepositoryMock.Object,
            _supplierRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _localizationMock.Object);

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidBatch_CreatesBatchSuccessfully()
    {
        // Arrange
        var dto = new CreateStockBatchDto(1, DateTime.UtcNow.AddDays(10), 50m, 100m, 1, 0m);
        var product = new Product("SKU-1", "Product", 100m, 10);
        var supplier = new Supplier("Supplier", "Contact", "123", "Email");

        _productRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _supplierRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(supplier);
        _mapperMock.Setup(m => m.Map<StockBatchResponseDto>(It.IsAny<StockBatch>())).Returns(new StockBatchResponseDto(1, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(10), 50m, 0m, 100m, 100m, 1, "Supplier"));

        var service = CreateService();

        // Act
        var result = await service.CreateAsync(dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _stockBatchRepositoryMock.Verify(x => x.AddAsync(It.IsAny<StockBatch>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var dto = new CreateStockBatchDto(1, DateTime.UtcNow.AddDays(10), 50m, 100m, 1, 0m);

        _productRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Product)null!);

        var service = CreateService();

        // Act
        var result = await service.CreateAsync(dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PRODUCT_NOT_FOUND");
    }

    [Fact]
    public async Task CreateAsync_SupplierNotFound_ReturnsFailure()
    {
        // Arrange
        var dto = new CreateStockBatchDto(1, DateTime.UtcNow.AddDays(10), 50m, 100m, 1, 0m);
        var product = new Product("SKU-1", "Product", 100m, 10);

        _productRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _supplierRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Supplier)null!);

        var service = CreateService();

        // Act
        var result = await service.CreateAsync(dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("SUPPLIER_NOT_FOUND");
    }

    [Fact]
    public async Task CreateAsync_InvalidData_ReturnsValidationFailure()
    {
        // Arrange
        // Passing unit cost -5m which will throw ArgumentException
        var dto = new CreateStockBatchDto(1, DateTime.UtcNow.AddDays(10), -50m, 100m, 1, 0m);
        var product = new Product("SKU-1", "Product", 100m, 10);
        var supplier = new Supplier("Supplier", "Contact", "123", "Email");

        _productRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _supplierRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(supplier);

        var service = CreateService();

        // Act
        var result = await service.CreateAsync(dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_BATCH_DATA");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidBatch_UpdatesSuccessfully()
    {
        // Arrange
        var dto = new UpdateStockBatchDto(DateTime.UtcNow.AddDays(20), 45m, 80m, 5m);
        var batch = new StockBatch(1, 1, DateTime.UtcNow.AddDays(10), 50m, 100m, 0m);

        _stockBatchRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(batch);
        _mapperMock.Setup(m => m.Map<StockBatchResponseDto>(It.IsAny<StockBatch>())).Returns(new StockBatchResponseDto(1, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(10), 50m, 0m, 100m, 100m, 1, "Supplier"));

        var service = CreateService();

        // Act
        var result = await service.UpdateAsync(1, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        batch.UnitCost.Should().Be(45m);
        batch.RemainingQuantity.Should().Be(80m);
        _stockBatchRepositoryMock.Verify(x => x.Update(batch), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_BatchNotFound_ReturnsFailure()
    {
        // Arrange
        var dto = new UpdateStockBatchDto(DateTime.UtcNow.AddDays(20), 45m, 80m, 5m);
        _stockBatchRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((StockBatch)null!);

        var service = CreateService();

        // Act
        var result = await service.UpdateAsync(1, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task UpdateAsync_InvalidData_ReturnsValidationFailure()
    {
        // Arrange
        var dto = new UpdateStockBatchDto(DateTime.UtcNow.AddDays(20), -45m, 80m, 5m); // Negative unit cost
        var batch = new StockBatch(1, 1, DateTime.UtcNow.AddDays(10), 50m, 100m, 0m);

        _stockBatchRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(batch);

        var service = CreateService();

        // Act
        var result = await service.UpdateAsync(1, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_BATCH_DATA");
    }

    #endregion

    #region Query Tests

    [Fact]
    public async Task GetAllAsync_Always_ReturnsMappedBatches()
    {
        // Arrange
        var batches = new List<StockBatch> { new StockBatch(1, 1, DateTime.UtcNow.AddDays(10), 50m, 100m, 0m) };
        _stockBatchRepositoryMock.Setup(x => x.GetAllWithDetailsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(batches);
        _mapperMock.Setup(m => m.Map<IEnumerable<StockBatchResponseDto>>(batches)).Returns(new List<StockBatchResponseDto> { new StockBatchResponseDto(1, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(10), 50m, 0m, 100m, 100m, 1, "Supplier") });

        var service = CreateService();

        // Act
        var result = await service.GetAllAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingBatch_ReturnsBatch()
    {
        // Arrange
        var batch = new StockBatch(1, 1, DateTime.UtcNow.AddDays(10), 50m, 100m, 0m);
        _stockBatchRepositoryMock.Setup(x => x.GetWithDetailsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(batch);
        _mapperMock.Setup(m => m.Map<StockBatchResponseDto>(batch)).Returns(new StockBatchResponseDto(1, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(10), 50m, 0m, 100m, 100m, 1, "Supplier"));

        var service = CreateService();

        // Act
        var result = await service.GetByIdAsync(1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingBatch_ReturnsFailure()
    {
        // Arrange
        _stockBatchRepositoryMock.Setup(x => x.GetWithDetailsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((StockBatch)null!);

        var service = CreateService();

        // Act
        var result = await service.GetByIdAsync(1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task GetByProductIdAsync_ExistingProductBatches_ReturnsBatches()
    {
        // Arrange
        var batches = new List<StockBatch> { new StockBatch(1, 1, DateTime.UtcNow.AddDays(10), 50m, 100m, 0m) };
        _stockBatchRepositoryMock.Setup(x => x.GetByProductIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(batches);
        _mapperMock.Setup(m => m.Map<IEnumerable<StockBatchResponseDto>>(batches)).Returns(new List<StockBatchResponseDto> { new StockBatchResponseDto(1, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(10), 50m, 0m, 100m, 100m, 1, "Supplier") });

        var service = CreateService();

        // Act
        var result = await service.GetByProductIdAsync(1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetBySupplierIdAsync_ExistingSupplierBatches_ReturnsBatches()
    {
        // Arrange
        var batches = new List<StockBatch> { new StockBatch(1, 1, DateTime.UtcNow.AddDays(10), 50m, 100m, 0m) };
        _stockBatchRepositoryMock.Setup(x => x.GetBySupplierIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(batches);
        _mapperMock.Setup(m => m.Map<IEnumerable<StockBatchResponseDto>>(batches)).Returns(new List<StockBatchResponseDto> { new StockBatchResponseDto(1, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(10), 50m, 0m, 100m, 100m, 1, "Supplier") });

        var service = CreateService();

        // Act
        var result = await service.GetBySupplierIdAsync(1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetExpiringBatchesAsync_BatchesExist_ReturnsMatchingBatches()
    {
        // Arrange
        var batches = new List<StockBatch> { new StockBatch(1, 1, DateTime.UtcNow.AddDays(5), 50m, 100m, 0m) };
        _stockBatchRepositoryMock.Setup(x => x.GetExpiringBatchesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(batches);
        _mapperMock.Setup(m => m.Map<IEnumerable<StockBatchResponseDto>>(batches)).Returns(new List<StockBatchResponseDto> { new StockBatchResponseDto(1, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(10), 50m, 0m, 100m, 100m, 1, "Supplier") });

        var service = CreateService();

        // Act
        var result = await service.GetExpiringBatchesAsync(10, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingBatch_DeletesSuccessfully()
    {
        // Arrange
        var batch = new StockBatch(1, 1, DateTime.UtcNow.AddDays(10), 50m, 100m, 0m);
        _stockBatchRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(batch);

        var service = CreateService();

        // Act
        var result = await service.DeleteAsync(1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _stockBatchRepositoryMock.Verify(x => x.Delete(batch), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_BatchNotFound_ReturnsFailure()
    {
        // Arrange
        _stockBatchRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((StockBatch)null!);

        var service = CreateService();

        // Act
        var result = await service.DeleteAsync(1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NOT_FOUND");
    }

    #endregion
}
