using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;
using Inventory.Application.DTOs.Category;
namespace Inventory.Application.Validation.Category
{
    public class UpdateCategoryImageDtoValidator : AbstractValidator<UpdateCategoryImageDto>
    {
        public UpdateCategoryImageDtoValidator()
        {
            RuleFor(x => x.Image)
                .NotNull().WithMessage("Image is required");
        }
    }
}
