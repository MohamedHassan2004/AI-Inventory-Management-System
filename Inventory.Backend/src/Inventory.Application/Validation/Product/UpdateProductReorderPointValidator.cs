using FluentValidation;
using Inventory.Application.DTOs.Product;
using Inventory.Application.Interfaces;

namespace Inventory.Application.Validation.Product
{
    public class UpdateProductReorderPointValidator : AbstractValidator<UpdateProductReorderPointDto>
    {
        public UpdateProductReorderPointValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.ReorderPoint)
                .GreaterThanOrEqualTo(0).WithMessage(localizationService.GetMessage("ProductReorderPointInvalid"));
        }
    }
}
