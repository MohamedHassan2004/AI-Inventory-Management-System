using FluentValidation;
using Inventory.Application.DTOs.Order;
using Inventory.Application.Interfaces;

namespace Inventory.Application.Validation.Order
{
    public class UpdateOrderItemValidator : AbstractValidator<UpdateOrderItemDto>
    {
        public UpdateOrderItemValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage("Quantity must be greater than zero.");
        }
    }
}
