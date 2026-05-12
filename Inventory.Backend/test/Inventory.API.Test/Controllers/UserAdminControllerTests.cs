using FluentAssertions;
using Inventory.API.Controllers;
using Inventory.Application.DTOs;
using Inventory.Application.Interfaces;
using Inventory.Application.Interfaces.Auth;
using Inventory.Domain.Enums;
using Inventory.Domain.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Inventory.API.Test.Controllers;

public class UserAdminControllerTests
{
    private readonly Mock<IUserAdminService> _userAdminServiceMock;
    private readonly Mock<ILocalizationService> _localizationMock;

    public UserAdminControllerTests()
    {
        _userAdminServiceMock = new Mock<IUserAdminService>();

        _localizationMock = new Mock<ILocalizationService>();
    }

    private UserAdminController CreateController()
    {
        var services = new ServiceCollection();

        services.AddSingleton(_localizationMock.Object);

        var serviceProvider = services.BuildServiceProvider();

        var controller = new UserAdminController(
            _userAdminServiceMock.Object);

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
    public async Task GetAllAccounts_AccountsExist_ReturnsOk()
    {
        // Arrange
        var accounts = new List<AccountDto>
        {
            new()
            {
                UserId = "user-1",
                UserName = "mohamed",
                AccountStatus = AccountStatus.Active
            }
        };

        _userAdminServiceMock
            .Setup(x => x.GetAllAccountsAsync())
            .ReturnsAsync(Result.Success(accounts));

        var controller = CreateController();

        // Act
        var result = await controller.GetAllAccounts();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeleteUser_DeleteSucceeds_ReturnsNoContent()
    {
        // Arrange
        _userAdminServiceMock
            .Setup(x => x.DeleteUserAsync("user-1"))
            .ReturnsAsync(Result.Success());

        var controller = CreateController();

        // Act
        var result = await controller.DeleteUser("user-1");

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ApproveAccount_ApproveSucceeds_ReturnsNoContent()
    {
        // Arrange
        _userAdminServiceMock
            .Setup(x => x.ApproveAccountAsync("user-1"))
            .ReturnsAsync(Result.Success());

        var controller = CreateController();

        // Act
        var result = await controller.ApproveAccount("user-1");

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RejectAccount_RejectSucceeds_ReturnsNoContent()
    {
        // Arrange
        _userAdminServiceMock
            .Setup(x => x.RejectAccountAsync(
                "user-1",
                "Invalid identity"))
            .ReturnsAsync(Result.Success());

        var controller = CreateController();

        // Act
        var result = await controller.RejectAccount(
            "user-1",
            "Invalid identity");

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }
}