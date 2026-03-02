using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;
using Inventory.Application.DTOs.Category;
namespace Inventory.Application.Validation.Category
{
    public class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
    {
        public UpdateCategoryDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(100);
        }
    }
}
