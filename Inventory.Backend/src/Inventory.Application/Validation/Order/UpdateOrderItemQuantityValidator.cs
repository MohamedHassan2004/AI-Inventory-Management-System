using FluentValidation;
using Inventory.Application.DTOs.Order;
using Inventory.Application.Interfaces;

namespace Inventory.Application.Validation.Order
{
    public class UpdateOrderItemQuantityValidator : AbstractValidator<UpdateOrderItemQuantityDto>
    {
        public UpdateOrderItemQuantityValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage(localizationService.GetMessage("InvalidQuantity"));
        }
    }
}