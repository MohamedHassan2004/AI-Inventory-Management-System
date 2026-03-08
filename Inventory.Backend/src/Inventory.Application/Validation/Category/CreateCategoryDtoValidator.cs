using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;
using Inventory.Application.DTOs.Category;
using Inventory.Application.Interfaces;

namespace Inventory.Application.Validation.Category
{
    public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
    {
        public CreateCategoryDtoValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(localizationService.GetMessage("CategoryNameRequired"))
                .MaximumLength(100).WithMessage(localizationService.GetMessage("CategoryNameMaxLength"));

            RuleFor(x => x.Image)
                .NotNull().WithMessage(localizationService.GetMessage("CategoryImageRequired"));
        }
    }
}
