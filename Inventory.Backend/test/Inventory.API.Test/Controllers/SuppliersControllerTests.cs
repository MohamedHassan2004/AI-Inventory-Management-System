using FluentAssertions;
using Inventory.API.Controllers;
using Inventory.Application.DTOs.Supplier;
using Inventory.Application.Interfaces;
using Inventory.Application.Interfaces.Services;
using Inventory.Domain.Shared;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Inventory.API.Tests.Controllers;

public class SuppliersControllerTests
{
    private readonly Mock<ISupplierService> _supplierServiceMock;
    private readonly Mock<ISupplierReportService> _supplierReportServiceMock;

    private readonly SuppliersController _controller;

    public SuppliersControllerTests()
    {
        _supplierServiceMock = new Mock<ISupplierService>();
        _supplierReportServiceMock = new Mock<ISupplierReportService>();

        _controller = new SuppliersController(
            _supplierServiceMock.Object,
            _supplierReportServiceMock.Object);
    }

    [Fact]
    public async Task GetSuppliers_ShouldReturnOkResult()
    {
        // Arrange
        var report = new PagedResult<SupplierReportItemDto>(
            new List<SupplierReportItemDto>
            {
                new()
                {
                    SupplierId = 1,
                    SupplierName = "Supplier 1"
                }
            }, 1, 10, 1);

        _supplierReportServiceMock
            .Setup(x => x.GetSuppliersReportAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetSuppliers(
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow,
            1,
            10);

        // Assert
        var okResult = result.Should()
            .BeOfType<OkObjectResult>()
            .Subject;

        okResult.Value.Should().Be(report);
    }

    [Fact]
    public async Task GetById_ShouldCallService()
    {
        // Arrange
        _supplierServiceMock
            .Setup(x => x.GetSupplierByIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<SupplierDto>(new SupplierDto(1, "x", "x", "x", "x", 1, 1, 1, 1, 1)));

        // Act
        await _controller.GetById(1, CancellationToken.None);

        // Assert
        _supplierServiceMock.Verify(
            x => x.GetSupplierByIdAsync(
                1,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Create_ShouldCallService()
    {
        // Arrange
        var dto = new CreateSupplierDto(
            "Supplier",
            "01012345678",
            null,
            null
        );

        _supplierServiceMock
            .Setup(x => x.CreateSupplierAsync(
                dto,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<SupplierDto>(new SupplierDto(1, "x", "x", "x", "x", 1, 1, 1, 1, 1)));

        // Act
        await _controller.Create(dto, CancellationToken.None);

        // Assert
        _supplierServiceMock.Verify(
            x => x.CreateSupplierAsync(
                dto,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Update_ShouldCallService()
    {
        // Arrange
        var dto = new UpdateSupplierDto(
            "Updated Supplier",
            "01111111111",
            null,
            null
        );

        _supplierServiceMock
            .Setup(x => x.UpdateSupplierAsync(
                1,
                dto,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<SupplierDto>(new SupplierDto(1, "x", "x", "x", "x", 1, 1, 1, 1, 1)));

        // Act
        await _controller.Update(
            1,
            dto,
            CancellationToken.None);

        // Assert
        _supplierServiceMock.Verify(
            x => x.UpdateSupplierAsync(
                1,
                dto,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Delete_ShouldCallService()
    {
        // Arrange
        _supplierServiceMock
            .Setup(x => x.DeleteSupplierAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _controller.Delete(1, CancellationToken.None);

        // Assert
        _supplierServiceMock.Verify(
            x => x.DeleteSupplierAsync(
                1,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Restore_ShouldCallService()
    {
        // Arrange
        _supplierServiceMock
            .Setup(x => x.RestoreSupplierAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _controller.Restore(1, CancellationToken.None);

        // Assert
        _supplierServiceMock.Verify(
            x => x.RestoreSupplierAsync(
                1,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetNotes_ShouldCallService()
    {
        // Arrange
        _supplierServiceMock
            .Setup(x => x.GetSupplierNotesAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IEnumerable<SupplierNoteDto>>(new List<SupplierNoteDto>()));

        // Act
        await _controller.GetNotes(1, CancellationToken.None);

        // Assert
        _supplierServiceMock.Verify(
            x => x.GetSupplierNotesAsync(
                1,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddRating_ShouldCallService()
    {
        // Arrange
        var dto = new AddSupplierRatingDto
        {
            Rating = 5,
            Note = "Excellent"
        };

        _supplierServiceMock
            .Setup(x => x.AddSupplierRatingAsync(
                1,
                dto,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<SupplierDto>(new SupplierDto(1, "x", "x", "x", "x", 1, 1, 1, 1, 1)));

        // Act
        await _controller.AddRating(
            1,
            dto,
            CancellationToken.None);

        // Assert
        _supplierServiceMock.Verify(
            x => x.AddSupplierRatingAsync(
                1,
                dto,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetDeleted_ShouldCallService()
    {
        // Arrange
        var deletedSuppliers = new List<SupplierDto>
        {
            new SupplierDto(1, "Deleted Supplier", "01000000000", null, null, 0, 0, 0, 0, 0)
        };

        _supplierServiceMock
            .Setup(x => x.GetDeletedSuppliersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IEnumerable<SupplierDto>>(deletedSuppliers));

        // Act
        var result = await _controller.GetDeleted(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(deletedSuppliers);

        _supplierServiceMock.Verify(
            x => x.GetDeletedSuppliersAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}