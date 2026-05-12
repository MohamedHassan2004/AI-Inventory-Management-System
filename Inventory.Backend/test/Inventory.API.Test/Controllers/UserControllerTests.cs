using FluentAssertions;
using Inventory.API.Controllers;
using Inventory.Application.DTOs.Auth;
using Inventory.Application.Interfaces;
using Inventory.Domain.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Inventory.API.Test.Controllers;

public class UserControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ILocalizationService> _localizationMock;

    public UserControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();

        _localizationMock = new Mock<ILocalizationService>();
    }

    private UserController CreateController()
    {
        var services = new ServiceCollection();

        services.AddSingleton(_localizationMock.Object);

        var serviceProvider = services.BuildServiceProvider();

        var controller = new UserController(
            _userServiceMock.Object);

        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        "user-123")
                },
                "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider,
                User = user
            }
        };

        return controller;
    }

    [Fact]
    public async Task GetUserStatus_StatusExists_ReturnsOk()
    {
        // Arrange
        _userServiceMock
            .Setup(x => x.GetUserStatusAsync("user-123"))
            .ReturnsAsync(Result.Success("Active"));

        var controller = CreateController();

        // Act
        var result = await controller.GetUserStatus();

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        var okResult = result as OkObjectResult;

        okResult!.Value.Should().Be("Active");
    }

    [Fact]
    public async Task GetRejectionReason_ReasonExists_ReturnsOk()
    {
        // Arrange
        _userServiceMock
            .Setup(x => x.GetIdentityRejectionReasonAsync("user-123"))
            .ReturnsAsync(Result.Success("Invalid identity image"));

        var controller = CreateController();

        // Act
        var result = await controller.GetRejectionReason();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UploadIdentityImage_UploadSucceeds_ReturnsNoContent()
    {
        // Arrange
        var dto = new UploadIdentityImgDto
        {
            IdentityImageFile = Mock.Of<IFormFile>()
        };

        _userServiceMock
            .Setup(x => x.UploadUserIdentityAsync(
                "user-123",
                dto))
            .ReturnsAsync(Result.Success());

        var controller = CreateController();

        // Act
        var result = await controller.UploadIdentityImage(dto);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }
}