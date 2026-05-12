using FluentAssertions;
using Inventory.API.Controllers;
using Inventory.Application.Interfaces;
using Inventory.Application.Interfaces.Auth;
using Inventory.Domain.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Inventory.API.Test.Controllers;

public class RoleControllerTests
{
    private readonly Mock<IRoleService> _roleServiceMock;
    private readonly Mock<ILocalizationService> _localizationMock;

    public RoleControllerTests()
    {
        _roleServiceMock = new Mock<IRoleService>();

        _localizationMock = new Mock<ILocalizationService>();
    }

    private RoleController CreateController()
    {
        var services = new ServiceCollection();

        services.AddSingleton(_localizationMock.Object);

        var serviceProvider = services.BuildServiceProvider();

        var controller = new RoleController(
            _roleServiceMock.Object);

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
    public async Task GetUserRoles_RolesExist_ReturnsOk()
    {
        // Arrange
        var roles = new List<string>
        {
            "Admin",
            "Cashier"
        };

        _roleServiceMock
            .Setup(x => x.GetUserRolesAsync("user-1"))
            .ReturnsAsync(Result.Success(roles));

        var controller = CreateController();

        // Act
        var result = await controller.GetUserRoles("user-1");

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        var okResult = result as OkObjectResult;

        okResult!.Value.Should().BeEquivalentTo(roles);
    }

    [Fact]
    public async Task AddUserRole_AddSucceeds_ReturnsNoContent()
    {
        // Arrange
        _roleServiceMock
            .Setup(x => x.AddUserRoleAsync(
                "user-1",
                "Admin"))
            .ReturnsAsync(Result.Success());

        var controller = CreateController();

        // Act
        var result = await controller.AddUserRole(
            "user-1",
            "Admin");

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RemoveUserRole_RemoveSucceeds_ReturnsNoContent()
    {
        // Arrange
        _roleServiceMock
            .Setup(x => x.RemoveUserRoleAsync(
                "user-1",
                "Admin"))
            .ReturnsAsync(Result.Success());

        var controller = CreateController();

        // Act
        var result = await controller.RemoveUserRole(
            "user-1",
            "Admin");

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }
}