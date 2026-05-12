using FluentAssertions;
using Inventory.API.Controllers;
using Inventory.Application.DTOs.Supplier;
using Inventory.Application.Interfaces;
using Inventory.Domain.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Inventory.API.Test.Controllers;

public class SuppliersControllerTests
{
    private readonly Mock<ISupplierService> _supplierServiceMock;
    private readonly Mock<ILocalizationService> _localizationMock;

    public SuppliersControllerTests()
    {
        _supplierServiceMock = new Mock<ISupplierService>();

        _localizationMock = new Mock<ILocalizationService>();
    }

    private SuppliersController CreateController()
    {
        var services = new ServiceCollection();
        services.AddControllers();
        services.AddSingleton(_localizationMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var controller = new SuppliersController(
            _supplierServiceMock.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider
            }
        };

        return controller;
    }

    [Fact]
    public async Task GetAll_Should_Return_Ok_When_Suppliers_Exist()
    {
        // Arrange
        var suppliers = new List<SupplierDto>
        {
            new(
                1,
                "Main Supplier",
                "01000000000",
                null,
                null,
                0,
                0,
                0,
                0,
                0)
        };

        _supplierServiceMock
            .Setup(x => x.GetAllSuppliersAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                Result.Success<IEnumerable<SupplierDto>>(suppliers));

        var controller = CreateController();

        // Act
        var result = await controller.GetAll(
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Should_Return_Ok_When_Supplier_Exists()
    {
        // Arrange
        var supplier = new SupplierDto(
            1,
            "Main Supplier",
            "01000000000",
            null,
            null,
            0,
            0,
            0,
            0,
            0);

        _supplierServiceMock
            .Setup(x => x.GetSupplierByIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(supplier));

        var controller = CreateController();

        // Act
        var result = await controller.GetById(
            1,
            CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(supplier);
    }

    [Fact]
    public async Task GetById_Should_Return_NotFound_When_Supplier_Does_Not_Exist()
    {
        // Arrange
        var error = new Error("Supplier.NotFound", "Supplier not found", ErrorType.NotFound);
        _supplierServiceMock
            .Setup(x => x.GetSupplierByIdAsync(
                99,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<SupplierDto>(error));

        _localizationMock
            .Setup(x => x.GetMessage(It.IsAny<string>()))
            .Returns("Not Found");

        var controller = CreateController();

        // Act
        var result = await controller.GetById(
            99,
            CancellationToken.None);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Create_Should_Return_BadRequest_When_Validation_Fails()
    {
        // Arrange
        var dto = new CreateSupplierDto("", "01000000000", null, null);
        var error = new Error("Supplier.Validation", "Invalid data", ErrorType.Validation);
        
        _supplierServiceMock
            .Setup(x => x.CreateSupplierAsync(
                dto,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<SupplierDto>(error));

        _localizationMock
            .Setup(x => x.GetMessage(It.IsAny<string>()))
            .Returns("Bad Request");

        var controller = CreateController();

        // Act
        var result = await controller.Create(
            dto,
            CancellationToken.None);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Create_Should_Return_Ok_When_Supplier_Is_Created()
    {
        // Arrange
        var dto = new CreateSupplierDto(
            "Main Supplier",
            "01000000000",
            null,
            null);

        var response = new SupplierDto(
            1,
            "Main Supplier",
            "01000000000",
            null,
            null,
            0,
            0,
            0,
            0,
            0);

        _supplierServiceMock
            .Setup(x => x.CreateSupplierAsync(
                dto,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(response));

        var controller = CreateController();

        // Act
        var result = await controller.Create(
            dto,
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_Should_Return_NoContent_When_Delete_Succeeds()
    {
        // Arrange
        _supplierServiceMock
            .Setup(x => x.DeleteSupplierAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var controller = CreateController();

        // Act
        var result = await controller.Delete(
            1,
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Restore_Should_Return_NoContent_When_Restore_Succeeds()
    {
        // Arrange
        _supplierServiceMock
            .Setup(x => x.RestoreSupplierAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var controller = CreateController();

        // Act
        var result = await controller.Restore(
            1,
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetNotes_Should_Return_Ok_When_Notes_Exist()
    {
        // Arrange
        var notes = new List<SupplierNoteDto>
        {
            new(
                1,
                "Good supplier",
                DateTime.UtcNow)
        };

        _supplierServiceMock
            .Setup(x => x.GetSupplierNotesAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                Result.Success<IEnumerable<SupplierNoteDto>>(notes));

        var controller = CreateController();

        // Act
        var result = await controller.GetNotes(
            1,
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AddRating_Should_Return_Ok_When_Rating_Is_Added()
    {
        // Arrange
        var dto = new AddSupplierRatingDto
        {
            Rating = 5,
            Note = "Excellent supplier"
        };

        var response = new SupplierDto(
            1,
            "Main Supplier",
            "01000000000",
            null,
            null,
            5,
            1,
            5,
            0,
            0);

        _supplierServiceMock
            .Setup(x => x.AddSupplierRatingAsync(
                1,
                dto,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(response));

        var controller = CreateController();

        // Act
        var result = await controller.AddRating(
            1,
            dto,
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}