using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;
using Inventory.Application.Interfaces;
using Inventory.Application.DTOs;
using Inventory.Application.DTOs.Category;
namespace Inventory.Application.Validation.Category
{
    public class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
    {
        public UpdateCategoryDtoValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(localizationService.GetMessage("CategoryNameRequired"))
                .MaximumLength(100).WithMessage(localizationService.GetMessage("CategoryNameMaxLength"));
        }
    }
}
