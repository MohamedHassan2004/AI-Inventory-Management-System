using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;
using Inventory.Application.DTOs.Category;
namespace Inventory.Application.Validation.Category
{
    public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
    {
        public CreateCategoryDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(100);

            RuleFor(x => x.Image)
                .NotNull().WithMessage("Image is required");
        }
    }
}
