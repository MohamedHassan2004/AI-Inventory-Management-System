using FluentAssertions;
using Inventory.Application.DTOs.Category;
using Inventory.Application.Interfaces;
using Inventory.Application.Services;
using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Shared;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Inventory.Application.Test.Service;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IFileService> _fileServiceMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<CategoryService>> _loggerMock = new();
    private readonly Mock<ILocalizationService> _localizationMock = new();

    public CategoryServiceTests()
    {
        _localizationMock
            .Setup(x => x.GetMessage(It.IsAny<string>()))
            .Returns<string>(key => key);
    }

    private CategoryService CreateService() =>
        new(
            _categoryRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _fileServiceMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _localizationMock.Object);

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_ValidCategory_CreatesCategorySuccessfully()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var dto = new CreateCategoryDto { Name = "Category 1", Image = fileMock.Object };

        _categoryRepositoryMock.Setup(x => x.ExistsByNameAsync(dto.Name, null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _fileServiceMock.Setup(x => x.SaveFileAsync(dto.Image, "categories")).ReturnsAsync(Result.Success("path/to/image.png"));
        _mapperMock.Setup(m => m.Map<CategoryResponseDto>(It.IsAny<Category>())).Returns(new CategoryResponseDto { Id = 1, Name = "Category 1", ImgUrl = "path/to/image.png" });

        var service = CreateService();

        // Act
        var result = await service.CreateAsync(dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _categoryRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateCategory_ReturnsFailure()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var dto = new CreateCategoryDto { Name = "Duplicate Category", Image = fileMock.Object };

        _categoryRepositoryMock.Setup(x => x.ExistsByNameAsync(dto.Name, null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CreateService();

        // Act
        var result = await service.CreateAsync(dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("DUPLICATED_NAME");
    }

    [Fact]
    public async Task CreateAsync_FileUploadFails_ReturnsFailure()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var dto = new CreateCategoryDto { Name = "Category 1", Image = fileMock.Object };

        _categoryRepositoryMock.Setup(x => x.ExistsByNameAsync(dto.Name, null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _fileServiceMock.Setup(x => x.SaveFileAsync(dto.Image, "categories")).ReturnsAsync(Result.Failure<string>(new Error("UPLOAD_FAILED", "Failed", ErrorType.Failure)));

        var service = CreateService();

        // Act
        var result = await service.CreateAsync(dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("UPLOAD_FAILED");
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_ExistingCategory_UpdatesSuccessfully()
    {
        // Arrange
        var category = new Category("Old Category", "path/to/image.png");
        var dto = new UpdateCategoryDto { Name = "New Category" };

        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _categoryRepositoryMock.Setup(x => x.ExistsByNameAsync(dto.Name, 1, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _mapperMock.Setup(m => m.Map<CategoryResponseDto>(It.IsAny<Category>())).Returns(new CategoryResponseDto { Id = 1, Name = "New Category", ImgUrl = "path/to/image.png" });

        var service = CreateService();

        // Act
        var result = await service.UpdateAsync(1, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        category.Name.Should().Be("New Category");
        _categoryRepositoryMock.Verify(x => x.Update(category), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CategoryNotFound_ReturnsFailure()
    {
        // Arrange
        var dto = new UpdateCategoryDto { Name = "New Category" };
        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Category)null!);

        var service = CreateService();

        // Act
        var result = await service.UpdateAsync(1, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task UpdateAsync_DuplicateName_ReturnsFailure()
    {
        // Arrange
        var category = new Category("Old Category", "path/to/image.png");
        var dto = new UpdateCategoryDto { Name = "Duplicate Category" };

        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _categoryRepositoryMock.Setup(x => x.ExistsByNameAsync(dto.Name, 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CreateService();

        // Act
        var result = await service.UpdateAsync(1, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("DUPLICATED_NAME");
    }

    #endregion

    #region UpdateCategoryImageAsync

    [Fact]
    public async Task UpdateCategoryImageAsync_ValidImage_UpdatesSuccessfully()
    {
        // Arrange
        var category = new Category("Category 1", "old/path.png");
        var fileMock = new Mock<IFormFile>();
        var dto = new UpdateCategoryImageDto { Image = fileMock.Object };

        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _fileServiceMock.Setup(x => x.SaveFileAsync(dto.Image, "categories")).ReturnsAsync(Result.Success("new/path.png"));

        var service = CreateService();

        // Act
        var result = await service.UpdateCategoryImageAsync(1, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        category.ImgUrl.Should().Be("new/path.png");
        _categoryRepositoryMock.Verify(x => x.Update(category), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _fileServiceMock.Verify(x => x.DeleteFileAsync("old/path.png"), Times.Once);
    }

    [Fact]
    public async Task UpdateCategoryImageAsync_CategoryNotFound_ReturnsFailure()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var dto = new UpdateCategoryImageDto { Image = fileMock.Object };

        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Category)null!);

        var service = CreateService();

        // Act
        var result = await service.UpdateCategoryImageAsync(1, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task UpdateCategoryImageAsync_FileUploadFails_ReturnsFailure()
    {
        // Arrange
        var category = new Category("Category 1", "old/path.png");
        var fileMock = new Mock<IFormFile>();
        var dto = new UpdateCategoryImageDto { Image = fileMock.Object };

        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _fileServiceMock.Setup(x => x.SaveFileAsync(dto.Image, "categories")).ReturnsAsync(Result.Failure<string>(new Error("UPLOAD_FAILED", "Failed", ErrorType.Failure)));

        var service = CreateService();

        // Act
        var result = await service.UpdateCategoryImageAsync(1, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("UPLOAD_FAILED");
    }

    #endregion

    #region Query Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllCategories()
    {
        // Arrange
        var categories = new List<Category> { new Category("Category 1", "path.png") };
        _categoryRepositoryMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(categories);
        _mapperMock.Setup(m => m.Map<IEnumerable<CategoryResponseDto>>(categories)).Returns(new List<CategoryResponseDto> { new CategoryResponseDto { Id = 1, Name = "Category 1", ImgUrl = "path.png" } });

        var service = CreateService();

        // Act
        var result = await service.GetAllAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_ExistingCategory_DeletesSuccessfully()
    {
        // Arrange
        var category = new Category("Category 1", "path.png");
        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        var service = CreateService();

        // Act
        var result = await service.DeleteAsync(1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        category.IsDeleted.Should().BeTrue();
        _categoryRepositoryMock.Verify(x => x.Update(category), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _fileServiceMock.Verify(x => x.DeleteFileAsync("path.png"), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_CategoryNotFound_ReturnsFailure()
    {
        // Arrange
        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Category)null!);

        var service = CreateService();

        // Act
        var result = await service.DeleteAsync(1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NOT_FOUND");
    }

    #endregion
}
