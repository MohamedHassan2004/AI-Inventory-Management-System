using FluentValidation;
using Inventory.Application.DTOs.Order;
using Inventory.Application.Interfaces;

namespace Inventory.Application.Validation.Order
{
    public class CreateOrderValidator : AbstractValidator<CreateOrderDto>
    {
        public CreateOrderValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.Items)
                .NotNull()
                .NotEmpty()
                .WithMessage(localizationService.GetMessage("OrderItemsRequired"));

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId)
                    .GreaterThan(0)
                    .WithMessage(localizationService.GetMessage("InvalidProductId"));

                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0)
                    .WithMessage(localizationService.GetMessage("InvalidQuantity"));
            });
        }
    }
}