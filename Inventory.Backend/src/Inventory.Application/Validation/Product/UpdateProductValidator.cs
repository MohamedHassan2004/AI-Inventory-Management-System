using FluentValidation;
using Inventory.Application.DTOs.Product;
using Inventory.Application.Interfaces;

namespace Inventory.Application.Validation.Product
{
    public class UpdateProductValidator : AbstractValidator<UpdateProductDto>
    {
        public UpdateProductValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(localizationService.GetMessage("ProductNameRequired"))
                .MaximumLength(200).WithMessage(localizationService.GetMessage("ProductNameMaxLength"));
        }
    }
}
