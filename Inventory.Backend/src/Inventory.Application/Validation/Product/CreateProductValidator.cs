using FluentValidation;
using Inventory.Application.DTOs.Product;
using Inventory.Application.Interfaces;

namespace Inventory.Application.Validation.Product
{
    public class CreateProductValidator : AbstractValidator<CreateProductDto>
    {
        public CreateProductValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.SKU)
                .NotEmpty().WithMessage(localizationService.GetMessage("ProductSkuRequired"))
                .MaximumLength(50).WithMessage(localizationService.GetMessage("ProductSkuMaxLength"));

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(localizationService.GetMessage("ProductNameRequired"))
                .MaximumLength(200).WithMessage(localizationService.GetMessage("ProductNameMaxLength"));

            RuleFor(x => x.SellingPrice)
                .GreaterThanOrEqualTo(0).WithMessage(localizationService.GetMessage("ProductPriceInvalid"));

            RuleFor(x => x.ReorderPoint)
                .GreaterThanOrEqualTo(0).WithMessage(localizationService.GetMessage("ProductReorderPointInvalid"));
        }
    }
}
