using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;
using Inventory.Application.Interfaces;
using Inventory.Application.DTOs;
using Inventory.Application.DTOs.Category;
namespace Inventory.Application.Validation.Category
{
    public class UpdateCategoryImageDtoValidator : AbstractValidator<UpdateCategoryImageDto>
    {
        public UpdateCategoryImageDtoValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.Image)
                .NotNull().WithMessage(localizationService.GetMessage("CategoryImageRequired"));
        }
    }
}
