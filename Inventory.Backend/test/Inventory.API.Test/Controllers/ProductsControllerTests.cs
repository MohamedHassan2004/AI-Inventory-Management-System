using FluentAssertions;
using Inventory.API.Controllers;
using Inventory.Application.DTOs.Product;
using Inventory.Application.Interfaces;
using Inventory.Domain.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Inventory.API.Test.Controllers;

public class ProductsControllerTests
{
    private readonly Mock<IProductService> _productServiceMock;
    private readonly Mock<ILocalizationService> _localizationMock;

    public ProductsControllerTests()
    {
        _productServiceMock = new Mock<IProductService>();

        _localizationMock = new Mock<ILocalizationService>();
    }

    private ProductsController CreateController()
    {
        var services = new ServiceCollection();
        services.AddControllers();
        services.AddSingleton(_localizationMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var controller = new ProductsController(
            _productServiceMock.Object);

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
    public async Task GetAll_ProductsExist_ReturnsOk()
    {
        // Arrange
        var products = new List<ProductResponseDto>
        {
            new(
                1,
                "P-001",
                "Laptop",
                1000,
                10,
                5,
                null)
        };

        _productServiceMock
            .Setup(x => x.GetAllAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                Result.Success<IEnumerable<ProductResponseDto>>(products));

        var controller = CreateController();

        // Act
        var result = await controller.GetAll(
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_ProductExists_ReturnsOk()
    {
        // Arrange
        var product = new ProductResponseDto(
            1,
            "P-001",
            "Laptop",
            1000,
            10,
            5,
            null);

        _productServiceMock
            .Setup(x => x.GetByIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(product));

        var controller = CreateController();

        // Act
        var result = await controller.GetById(
            1,
            CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(product);
    }

    [Fact]
    public async Task GetById_ProductDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var error = new Error("Product.NotFound", "Product not found", ErrorType.NotFound);
        _productServiceMock
            .Setup(x => x.GetByIdAsync(
                99,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ProductResponseDto>(error));

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
    public async Task Create_ValidationFails_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateProductDto("P-001", "", 1000, 10, null);
        var error = new Error("Product.Validation", "Invalid data", ErrorType.Validation);
        
        _productServiceMock
            .Setup(x => x.CreateAsync(
                dto,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ProductResponseDto>(error));

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
    public async Task Delete_DeleteSucceeds_ReturnsNoContent()
    {
        // Arrange
        _productServiceMock
            .Setup(x => x.DeleteAsync(
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
    public async Task Search_QueryIsEmpty_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.Search(
            "",
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Search_QueryIsValid_ReturnsOk()
    {
        // Arrange
        var products = new List<ProductLookupDto>
        {
            new(
                1,
                "P-001",
                "Laptop",
                1000,
                10)
        };

        _productServiceMock
            .Setup(x => x.SearchAsync(
                "Laptop",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                Result.Success<IEnumerable<ProductLookupDto>>(products));

        var controller = CreateController();

        // Act
        var result = await controller.Search(
            "Laptop",
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}