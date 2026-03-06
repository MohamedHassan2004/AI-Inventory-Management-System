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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileService _fileService;
        private readonly ILogger<CategoryService> _logger;
        private readonly IMapper _mapper;

        public CategoryService(
            IUnitOfWork unitOfWork,
            IFileService fileService,
            IMapper mapper,
            ILogger<CategoryService> logger)
        {
            _unitOfWork = unitOfWork;
            _fileService = fileService;
            _mapper = mapper;
            _logger = logger;
        }
        public async Task<Result<CategoryResponseDto>> CreateAsync(CreateCategoryDto dto)
        {
            _logger.LogInformation("Creating category with name: {Name}", dto.Name);

            // 1️⃣ Check duplicate name
            if (await _unitOfWork.Categories.ExistsByNameAsync(dto.Name))
            {
                _logger.LogWarning("Duplicate category name detected: {Name}", dto.Name);

                return Result.Failure<CategoryResponseDto>(
                    "DUPLICATED_NAME",
                    "Category name already exists");
            }

            // 2️⃣ Save image
            var fileResult = await _fileService.SaveFileAsync(dto.Image, "categories");

            if (!fileResult.IsSuccess)
            {
                return Result.Failure<CategoryResponseDto>(
                    fileResult.ErrorCode,
                    fileResult.Message);
            }

            // 3️⃣ Create entity
            var category = new Category(dto.Name, fileResult.Value);

            // 4️⃣ Add
            await _unitOfWork.Categories.AddAsync(category);

            // 5️⃣ Commit
            await _unitOfWork.SaveChangesAsync();

            var response = _mapper.Map<CategoryResponseDto>(category);

            return Result.Success(response, "Category created successfully");
        }
        public async Task<Result<CategoryResponseDto>> UpdateAsync(int id, UpdateCategoryDto dto)
        {
            _logger.LogInformation("Updating category with Id: {Id}", id);

            // 1️⃣ نتأكد إنها موجودة
            var category = await _unitOfWork.Categories.GetByIdAsync(id);

            if (category is null)
            {
                _logger.LogWarning("Category not found with Id: {Id}", id);

                return Result.Failure<CategoryResponseDto>(
                    "NOT_FOUND",
                    "Category not found");
            }

            if (await _unitOfWork.Categories.ExistsByNameAsync(dto.Name, id))
            {
                _logger.LogWarning("Duplicate category name detected: {Name}", dto.Name);

                return Result.Failure<CategoryResponseDto>(
                    "DUPLICATED_NAME",
                    "Category name already exists");
            }

            // 3️⃣ نعدل الاسم
            category.UpdateName(dto.Name);

            // 4️⃣ نعمل Update
            _unitOfWork.Categories.Update(category);

            // 5️⃣ نحفظ
            await _unitOfWork.SaveChangesAsync();

            // 6️⃣ نرجع النتيجة
            var response = _mapper.Map<CategoryResponseDto>(category);

            return Result.Success(response, "Category updated successfully");
        }
        public async Task<Result> UpdateCategoryImageAsync(int id, UpdateCategoryImageDto dto)
        {
            _logger.LogInformation("Updating image for category Id: {Id}", id);

            // 1️⃣ التأكد من وجود الكاتيجوري
            var category = await _unitOfWork.Categories.GetByIdAsync(id);

            if (category is null)
            {
                _logger.LogWarning("Category not found with Id: {Id}", id);

                return Result.Failure(
                    "NOT_FOUND",
                    "Category not found");
            }

            // 2️⃣ حفظ الصورة الجديدة
            var fileResult = await _fileService.SaveFileAsync(dto.Image, "categories");

            if (!fileResult.IsSuccess)
            {
                _logger.LogWarning("Image upload failed: {Message}", fileResult.Message);

                return Result.Failure(
                    fileResult.ErrorCode,
                    fileResult.Message);
            }

            var oldImagePath = category.ImgUrl;

            // 3️⃣ تحديث الكيان
            category.UpdateImage(fileResult.Value);

            _unitOfWork.Categories.Update(category);

            // 4️⃣ حفظ التغييرات
            await _unitOfWork.SaveChangesAsync();

            // 5️⃣ حذف الصورة القديمة (بعد نجاح الحفظ)
            if (!string.IsNullOrWhiteSpace(oldImagePath))
            {
                await _fileService.DeleteFileAsync(oldImagePath);
            }

            _logger.LogInformation("Category image updated successfully for Id: {Id}", id);

            return Result.Success("Category image updated successfully");
        }
        public async Task<Result<IEnumerable<CategoryResponseDto>>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all categories");

            var categories = await _unitOfWork.Categories.GetAllAsync();

            var response = _mapper.Map<IEnumerable<CategoryResponseDto>>(categories);

            return Result.Success<IEnumerable<CategoryResponseDto>>(response);
        }
        public async Task<Result> DeleteAsync(int id)
        {
            _logger.LogInformation("Soft deleting category with Id: {Id}", id);

            // 1️⃣ Check existence
            var category = await _unitOfWork.Categories.GetByIdAsync(id);

            if (category is null)
            {
                _logger.LogWarning("Category not found with Id: {Id}", id);

                return Result.Failure(
                    "NOT_FOUND",
                    "Category not found");
            }

            // 2️⃣ Soft Delete
            category.Delete() ;

            _unitOfWork.Categories.Update(category);

            // 3️⃣ Save changes
            await _unitOfWork.SaveChangesAsync();

            // 4️⃣ Delete image after success
            if (!string.IsNullOrWhiteSpace(category.ImgUrl))
            {
                await _fileService.DeleteFileAsync(category.ImgUrl);
            }

            _logger.LogInformation("Category soft deleted successfully with Id: {Id}", id);

            return Result.Success("Category deleted successfully");
        }
    }
}
