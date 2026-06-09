using FluentAssertions;
using Inventory.Application.DTOs.Supplier;
using Inventory.Application.Interfaces;
using Inventory.Application.Services;
using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Shared;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Inventory.Application.Test.Service;

public class SupplierServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ISupplierRepository> _supplierRepositoryMock = new();
    private readonly Mock<ILocalizationService> _localizationMock = new();

    public SupplierServiceTests()
    {
        _localizationMock
            .Setup(x => x.GetMessage(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns<string, object[]>((key, args) => key);
        
        _localizationMock
            .Setup(x => x.GetMessage(It.IsAny<string>()))
            .Returns<string>(key => key);
    }

    private SupplierService CreateService() =>
        new(
            _unitOfWorkMock.Object,
            _supplierRepositoryMock.Object,
            _localizationMock.Object);

    #region CreateSupplierAsync

    [Fact]
    public async Task CreateSupplierAsync_ValidSupplier_CreatesSupplierSuccessfully()
    {
        // Arrange
        var dto = new CreateSupplierDto("Supplier 1", "123456789", "Contact info", "Address");
        _supplierRepositoryMock.Setup(x => x.ExistsAsync(dto.Name, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var service = CreateService();

        // Act
        var result = await service.CreateSupplierAsync(dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Name.Should().Be("Supplier 1");
        _supplierRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Supplier>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateSupplierAsync_DuplicateSupplier_ReturnsFailure()
    {
        // Arrange
        var dto = new CreateSupplierDto("Duplicate Supplier", "123456789", null, null);
        _supplierRepositoryMock.Setup(x => x.ExistsAsync(dto.Name, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CreateService();

        // Act
        var result = await service.CreateSupplierAsync(dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Supplier.AlreadyExists");
    }

    #endregion

    #region UpdateSupplierAsync

    [Fact]
    public async Task UpdateSupplierAsync_ExistingSupplier_UpdatesSuccessfully()
    {
        // Arrange
        var supplier = new Supplier("Old Supplier", "123456789", null, null);
        var dto = new UpdateSupplierDto("New Supplier", "987654321", null, null);

        _supplierRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(supplier);
        _supplierRepositoryMock.Setup(x => x.ExistsAsync(dto.Name, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var service = CreateService();

        // Act
        var result = await service.UpdateSupplierAsync(1, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        supplier.Name.Should().Be("New Supplier");
        supplier.PhoneNumber.Should().Be("987654321");
        _supplierRepositoryMock.Verify(x => x.Update(supplier), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSupplierAsync_SupplierNotFound_ReturnsFailure()
    {
        // Arrange
        var dto = new UpdateSupplierDto("New Supplier", "987654321", null, null);
        _supplierRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Supplier)null!);

        var service = CreateService();

        // Act
        var result = await service.UpdateSupplierAsync(1, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Supplier.NotFound");
    }

    [Fact]
    public async Task UpdateSupplierAsync_DuplicateName_ReturnsFailure()
    {
        // Arrange
        var supplier = new Supplier("Old Supplier", "123456789", null, null);
        var dto = new UpdateSupplierDto("Duplicate Supplier", "987654321", null, null);

        _supplierRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(supplier);
        _supplierRepositoryMock.Setup(x => x.ExistsAsync(dto.Name, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CreateService();

        // Act
        var result = await service.UpdateSupplierAsync(1, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Supplier.AlreadyExists");
    }

    #endregion

    #region DeleteSupplierAsync

    [Fact]
    public async Task DeleteSupplierAsync_ExistingSupplier_DeletesSuccessfully()
    {
        // Arrange
        var supplier = new Supplier("Supplier", "123456789", null, null);
        _supplierRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(supplier);
        _supplierRepositoryMock.Setup(x => x.HasRelatedStockBatchesAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var service = CreateService();

        // Act
        var result = await service.DeleteSupplierAsync(1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        supplier.IsDeleted.Should().BeTrue();
        _supplierRepositoryMock.Verify(x => x.Update(supplier), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteSupplierAsync_SupplierNotFound_ReturnsFailure()
    {
        // Arrange
        _supplierRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Supplier)null!);

        var service = CreateService();

        // Act
        var result = await service.DeleteSupplierAsync(1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Supplier.NotFound");
    }

    [Fact]
    public async Task DeleteSupplierAsync_HasStockBatches_ReturnsFailure()
    {
        // Arrange
        var supplier = new Supplier("Supplier", "123456789", null, null);
        _supplierRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(supplier);
        _supplierRepositoryMock.Setup(x => x.HasRelatedStockBatchesAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CreateService();

        // Act
        var result = await service.DeleteSupplierAsync(1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Supplier.HasStockBatches");
    }

    #endregion

    #region Query Tests

    [Fact]
    public async Task GetSupplierByIdAsync_ExistingSupplier_ReturnsSupplier()
    {
        // Arrange
        var supplier = new Supplier("Supplier", "123456789", null, null);
        _supplierRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(supplier);

        var service = CreateService();

        // Act
        var result = await service.GetSupplierByIdAsync(1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Name.Should().Be("Supplier");
    }

    [Fact]
    public async Task GetSupplierByIdAsync_SupplierNotFound_ReturnsFailure()
    {
        // Arrange
        _supplierRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Supplier)null!);

        var service = CreateService();

        // Act
        var result = await service.GetSupplierByIdAsync(1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Supplier.NotFound");
    }

    [Fact]
    public async Task GetAllSuppliersAsync_ReturnsAllSuppliers()
    {
        // Arrange
        var suppliers = new List<Supplier> { new Supplier("Supplier 1", "123", null, null) };
        _supplierRepositoryMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(suppliers);

        var service = CreateService();

        // Act
        var result = await service.GetAllSuppliersAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    #endregion
}
