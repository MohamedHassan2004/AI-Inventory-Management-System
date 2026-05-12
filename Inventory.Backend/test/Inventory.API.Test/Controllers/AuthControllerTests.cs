using FluentAssertions;
using Inventory.API.Controllers;
using Inventory.API.Settings;
using Inventory.Application.DTOs.Auth;
using Inventory.Application.Interfaces;
using Inventory.Application.Interfaces.Auth;
using Inventory.Domain.Shared;
using Inventory.Infrastructure.Data.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Inventory.API.Test.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<ILocalizationService> _localizationMock;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();

        _localizationMock = new Mock<ILocalizationService>();
    }

    private AuthController CreateController()
    {
        var services = new ServiceCollection();
        services.AddControllers();
        services.AddSingleton(_localizationMock.Object);

        var serviceProvider = services.BuildServiceProvider();

        var refreshTokenOptions = Options.Create(
            new RefreshTokenCookieSettings());

        var jwtOptions = Options.Create(
            new JwtSettings
            {
                RefreshTokenDurationInDays = 7
            });

        var controller = new AuthController(
            _authServiceMock.Object,
            _localizationMock.Object,
            refreshTokenOptions,
            jwtOptions);

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
    public async Task Login_LoginSucceeds_ReturnsOk()
    {
        // Arrange
        var dto = new LoginDto
        {
            UserName = "mohamed",
            Password = "Password123"
        };

        var token = new TokenDto(
            "access-token",
            "refresh-token");

        _authServiceMock
            .Setup(x => x.LoginAsync(dto))
            .ReturnsAsync(Result.Success(token));

        var controller = CreateController();

        // Act
        var result = await controller.Login(dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(token);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var dto = new LoginDto { UserName = "invalid", Password = "wrong" };
        var error = new Error("Auth.InvalidCredentials", "Invalid username or password", ErrorType.Unauthorized);

        _authServiceMock
            .Setup(x => x.LoginAsync(dto))
            .ReturnsAsync(Result.Failure<TokenDto>(error));

        _localizationMock
            .Setup(x => x.GetMessage(It.IsAny<string>()))
            .Returns("Unauthorized");

        var controller = CreateController();

        // Act
        var result = await controller.Login(dto);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task Logout_LogoutSucceeds_ReturnsOk()
    {
        // Arrange
        _localizationMock
            .Setup(x => x.GetMessage("LogoutSuccess"))
            .Returns("Logout successful");

        _authServiceMock
            .Setup(x => x.LogoutAsync("user-123"))
            .Returns(Task.CompletedTask);

        var controller = CreateController();

        // Act
        var result = await controller.Logout();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RefreshToken_CookieMissing_ReturnsUnauthorized()
    {
        // Arrange
        _localizationMock
            .Setup(x => x.GetMessage("InvalidRefreshToken"))
            .Returns("Invalid refresh token");

        var controller = CreateController();

        // Act
        var result = await controller.RefreshToken();

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task ChangePassword_ChangeSucceeds_ReturnsNoContent()
    {
        // Arrange
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "OldPassword123",
            NewPassword = "NewPassword123",
            ConfirmNewPassword = "NewPassword123"
        };

        _authServiceMock
            .Setup(x => x.ChangePasswordAsync(
                "user-123",
                dto))
            .ReturnsAsync(Result.Success());

        var controller = CreateController();

        // Act
        var result = await controller.ChangePassword(dto);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ChangePassword_ValidationFails_ReturnsBadRequest()
    {
        // Arrange
        var dto = new ChangePasswordDto { NewPassword = "123" };
        var error = new Error("Auth.Validation", "Invalid password format", ErrorType.Validation);

        _authServiceMock
            .Setup(x => x.ChangePasswordAsync("user-123", dto))
            .ReturnsAsync(Result.Failure(error));

        _localizationMock
            .Setup(x => x.GetMessage(It.IsAny<string>()))
            .Returns("Bad Request");

        var controller = CreateController();

        // Act
        var result = await controller.ChangePassword(dto);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }
}