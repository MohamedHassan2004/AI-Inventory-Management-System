using FluentValidation;
using Inventory.Application.DTOs.Product;
using Inventory.Application.Interfaces;

namespace Inventory.Application.Validation.Product
{
    public class UpdateProductPriceValidator : AbstractValidator<UpdateProductPriceDto>
    {
        public UpdateProductPriceValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.SellingPrice)
                .GreaterThanOrEqualTo(0).WithMessage(localizationService.GetMessage("ProductPriceInvalid"));
        }
    }
}
