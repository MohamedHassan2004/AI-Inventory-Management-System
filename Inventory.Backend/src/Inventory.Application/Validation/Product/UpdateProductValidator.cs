using FluentValidation;
using Inventory.Application.DTOs.Product;
using Inventory.Application.Interfaces;

namespace Inventory.Application.Validation.Product
{
    public class UpdateProductValidator : AbstractValidator<UpdateProductDto>
    {
        public UpdateProductValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.SKU)
                .NotEmpty().WithMessage(localizationService.GetMessage("ProductSkuRequired"))
                .MaximumLength(50).WithMessage(localizationService.GetMessage("ProductSkuMaxLength"));

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(localizationService.GetMessage("ProductNameRequired"))
                .MaximumLength(200).WithMessage(localizationService.GetMessage("ProductNameMaxLength"));
        }
    }
}
