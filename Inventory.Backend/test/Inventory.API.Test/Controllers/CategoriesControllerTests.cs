using FluentAssertions;
using Inventory.API.Controllers;
using Inventory.Application.DTOs.Category;
using Inventory.Application.Interfaces;
using Inventory.Domain.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Inventory.API.Test.Controllers;

public class CategoriesControllerTests
{
    private readonly Mock<ICategoryService> _categoryServiceMock;
    private readonly Mock<ILocalizationService> _localizationMock;

    public CategoriesControllerTests()
    {
        _categoryServiceMock = new Mock<ICategoryService>();
        _localizationMock = new Mock<ILocalizationService>();
    }

    private CategoriesController CreateController()
    {
        var services = new ServiceCollection();

        services.AddControllers();

        services.AddSingleton(_localizationMock.Object);

        var serviceProvider = services.BuildServiceProvider();

        var controller = new CategoriesController(
            _categoryServiceMock.Object);

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
    public async Task GetAll_Should_Return_Ok_When_Categories_Exist()
    {
        // Arrange
        var categories = new List<CategoryResponseDto>
        {
            new()
            {
                Id = 1,
                Name = "Electronics",
                ImgUrl = "image.jpg"
            }
        };

        _categoryServiceMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IEnumerable<CategoryResponseDto>>(categories));

        var controller = CreateController();

        // Act
        var result = await controller.GetAll(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        var okResult = result as OkObjectResult;

        okResult!.Value.Should().BeEquivalentTo(categories);
    }

    [Fact]
    public async Task Create_Should_Return_Ok_When_Category_Is_Created()
    {
        // Arrange
        var dto = new CreateCategoryDto
        {
            Name = "Electronics"
        };

        var response = new CategoryResponseDto
        {
            Id = 1,
            Name = "Electronics",
            ImgUrl = "image.jpg"
        };

        _categoryServiceMock
            .Setup(x => x.CreateAsync(
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
        _categoryServiceMock
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
    public async Task Delete_Should_Return_NotFound_When_Category_Does_Not_Exist()
    {
        // Arrange
        _localizationMock
            .Setup(x => x.GetMessage(It.IsAny<string>()))
            .Returns("Not Found");

        _categoryServiceMock
            .Setup(x => x.DeleteAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new Error(
                "NOT_FOUND",
                "Category not found",
                ErrorType.NotFound)));

        var controller = CreateController();

        // Act
        var result = await controller.Delete(
            1,
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();

        var objectResult = result as ObjectResult;

        objectResult!.StatusCode.Should().Be(404);
    }
}