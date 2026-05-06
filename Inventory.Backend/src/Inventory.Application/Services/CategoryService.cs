using Inventory.Application.DTOs.Category;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Shared;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Services
{
    public class CategoryService : ICategoryService
    {
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
        private readonly IFileService _fileService;
        private readonly ILogger<CategoryService> _logger;
        private readonly IMapper _mapper;
        private readonly ILocalizationService _localizationService;

        public CategoryService(
            ICategoryRepository categoryRepository,
            IUnitOfWork unitOfWork,
            IFileService fileService,
            IMapper mapper,
            ILogger<CategoryService> logger,
            ILocalizationService localizationService)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
            _fileService = fileService;
            _mapper = mapper;
            _logger = logger;
            _localizationService = localizationService;
        }
        public async Task<Result<CategoryResponseDto>> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating category with name: {Name}", dto.Name);

            
            if (await _categoryRepository.ExistsByNameAsync(dto.Name, null, cancellationToken))
            {
                _logger.LogWarning("Duplicate category name detected: {Name}", dto.Name);

                return Result.Failure<CategoryResponseDto>(new Error(
                    "DUPLICATED_NAME",
                    _localizationService.GetMessage("CategoryDuplicateName"),
                    ErrorType.Conflict));
            }

            
            var fileResult = await _fileService.SaveFileAsync(dto.Image, "categories");

            if (!fileResult.IsSuccess)
            {
                return Result.Failure<CategoryResponseDto>(fileResult.Error);
            }

            
            Category category;

            try
            {
                category = new Category(dto.Name, fileResult.Value);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid category data provided. Name: {Name}", dto.Name);

                return Result.Failure<CategoryResponseDto>(new Error(
                    "INVALID_CATEGORY_DATA",
                    _localizationService.GetMessage("InvalidCategoryData"),
                    ErrorType.Validation));
            }
            
            await _categoryRepository.AddAsync(category, cancellationToken);

            
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = _mapper.Map<CategoryResponseDto>(category);

            return Result.Success(response);
        }
        public async Task<Result<CategoryResponseDto>> UpdateAsync(int id, UpdateCategoryDto dto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating category with Id: {Id}", id);

            
            var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);

            if (category is null)
            {
                _logger.LogWarning("Category not found with Id: {Id}", id);

                return Result.Failure<CategoryResponseDto>(new Error(
                    "NOT_FOUND",
                    _localizationService.GetMessage("CategoryNotFound"),
                    ErrorType.NotFound));
            }

            if (await _categoryRepository.ExistsByNameAsync(dto.Name, id, cancellationToken))
            {
                _logger.LogWarning("Duplicate category name detected: {Name}", dto.Name);

                return Result.Failure<CategoryResponseDto>(new Error(
                    "DUPLICATED_NAME",
                    _localizationService.GetMessage("CategoryDuplicateName"),
                    ErrorType.Conflict));
            }

            
            try
            {
                category.UpdateName(dto.Name);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "Invalid category name provided while updating category. Id: {Id}, Name: {Name}",
                    id, dto.Name);

                return Result.Failure<CategoryResponseDto>(new Error(
                    "INVALID_CATEGORY_NAME",
                    _localizationService.GetMessage("InvalidCategoryData"),
                    ErrorType.Validation));
            }
            
            _categoryRepository.Update(category);

            
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            
            var response = _mapper.Map<CategoryResponseDto>(category);

            return Result.Success(response);
        }
        public async Task<Result> UpdateCategoryImageAsync(int id, UpdateCategoryImageDto dto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating image for category Id: {Id}", id);

            
            var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);

            if (category is null)
            {
                _logger.LogWarning("Category not found with Id: {Id}", id);

                return Result.Failure(new Error(
                    "NOT_FOUND",
                    _localizationService.GetMessage("CategoryNotFound"),
                    ErrorType.NotFound));
            }

            
            var fileResult = await _fileService.SaveFileAsync(dto.Image, "categories");

            if (!fileResult.IsSuccess)
            {
                _logger.LogWarning("Image upload failed: {Message}", fileResult.Error.Description);

                return Result.Failure(fileResult.Error);
            }

            var oldImagePath = category.ImgUrl;

            
            try
            {
                category.UpdateImage(fileResult.Value);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "Invalid image path provided while updating category image. Id: {Id}, ImagePath: {ImagePath}",
                    id, fileResult.Value);

                return Result.Failure(new Error(
                    "INVALID_IMAGE_PATH",
                    _localizationService.GetMessage("InvalidCategoryImage"),
                    ErrorType.Validation));
            }
            _categoryRepository.Update(category);

            
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            
            if (!string.IsNullOrWhiteSpace(oldImagePath))
            {
                await _fileService.DeleteFileAsync(oldImagePath);
            }

            _logger.LogInformation("Category image updated successfully for Id: {Id}", id);

            return Result.Success();
        }
        public async Task<Result<IEnumerable<CategoryResponseDto>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching all categories");

            var categories = await _categoryRepository.GetAllAsync(cancellationToken);

            var response = _mapper.Map<IEnumerable<CategoryResponseDto>>(categories);

            return Result.Success<IEnumerable<CategoryResponseDto>>(response);
        }
        public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Soft deleting category with Id: {Id}", id);

            
            var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);

            if (category is null)
            {
                _logger.LogWarning("Category not found with Id: {Id}", id);

                return Result.Failure(new Error(
                    "NOT_FOUND",
                    _localizationService.GetMessage("CategoryNotFound"),
                    ErrorType.NotFound));
            }

            
            category.Delete() ;

            _categoryRepository.Update(category);

            
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            
            if (!string.IsNullOrWhiteSpace(category.ImgUrl))
            {
                await _fileService.DeleteFileAsync(category.ImgUrl);
            }

            _logger.LogInformation("Category soft deleted successfully with Id: {Id}", id);

            return Result.Success();
        }
    }
}
