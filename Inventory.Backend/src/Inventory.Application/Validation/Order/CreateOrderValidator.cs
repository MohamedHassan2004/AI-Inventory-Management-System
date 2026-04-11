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
                .WithMessage(localizationService.GetMessage("OrderItemsRequired")
                             ?? "Order must contain at least one item");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId)
                    .GreaterThan(0)
                    .WithMessage(localizationService.GetMessage("InvalidProductId")
                                 ?? "Invalid product Id");

                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0)
                    .WithMessage(localizationService.GetMessage("InvalidQuantity")
                                 ?? "Quantity must be greater than zero");
            });
        }
    }
}