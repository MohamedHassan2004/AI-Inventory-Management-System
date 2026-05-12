using FluentAssertions;
using Inventory.API.Controllers;
using Inventory.Application.DTOs.StockBatch;
using Inventory.Application.Interfaces;
using Inventory.Domain.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Inventory.API.Test.Controllers;

public class StockBatchesControllerTests
{
    private readonly Mock<IStockBatchService> _stockBatchServiceMock;
    private readonly Mock<ILocalizationService> _localizationMock;

    public StockBatchesControllerTests()
    {
        _stockBatchServiceMock = new Mock<IStockBatchService>();

        _localizationMock = new Mock<ILocalizationService>();
    }

    private StockBatchesController CreateController()
    {
        var services = new ServiceCollection();

        services.AddSingleton(_localizationMock.Object);

        var serviceProvider = services.BuildServiceProvider();

        var controller = new StockBatchesController(
            _stockBatchServiceMock.Object);

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
    public async Task GetAll_Should_Return_Ok_When_Batches_Exist()
    {
        // Arrange
        var batches = new List<StockBatchResponseDto>
        {
            new(
                1,
                1,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(30),
                100,
                50,
                50,
                1)
        };

        _stockBatchServiceMock
            .Setup(x => x.GetAllAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                Result.Success<IEnumerable<StockBatchResponseDto>>(batches));

        var controller = CreateController();

        // Act
        var result = await controller.GetAll();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Should_Return_Ok_When_Batch_Exists()
    {
        // Arrange
        var batch = new StockBatchResponseDto(
            1,
            1,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30),
            100,
            50,
            50,
            1);

        _stockBatchServiceMock
            .Setup(x => x.GetByIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(batch));

        var controller = CreateController();

        // Act
        var result = await controller.GetById(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_Should_Return_Ok_When_Batch_Is_Created()
    {
        // Arrange
        var dto = new CreateStockBatchDto(
            1,
            DateTime.UtcNow.AddDays(30),
            100,
            50,
            1);

        var response = new StockBatchResponseDto(
            1,
            1,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30),
            100,
            50,
            50,
            1);

        _stockBatchServiceMock
            .Setup(x => x.CreateAsync(
                dto,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(response));

        var controller = CreateController();

        // Act
        var result = await controller.Create(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_Should_Return_NoContent_When_Delete_Succeeds()
    {
        // Arrange
        _stockBatchServiceMock
            .Setup(x => x.DeleteAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var controller = CreateController();

        // Act
        var result = await controller.Delete(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetExpiringBatches_Should_Return_Ok_When_Batches_Exist()
    {
        // Arrange
        var batches = new List<StockBatchResponseDto>
        {
            new(
                1,
                1,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(5),
                100,
                50,
                50,
                1)
        };

        _stockBatchServiceMock
            .Setup(x => x.GetExpiringBatchesAsync(
                7,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                Result.Success<IEnumerable<StockBatchResponseDto>>(batches));

        var controller = CreateController();

        // Act
        var result = await controller.GetExpiringBatches(7);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}