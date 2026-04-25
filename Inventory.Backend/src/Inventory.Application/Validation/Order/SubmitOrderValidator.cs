using FluentValidation;
using Inventory.Application.DTOs.Order;
using Inventory.Application.Interfaces;

namespace Inventory.Application.Validation.Order
{
    public class SubmitOrderValidator : AbstractValidator<SubmitOrderDto>
    {
        public SubmitOrderValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.Items)
                .NotEmpty()
                .WithMessage(localizationService.GetMessage("EmptyOrder") ?? "Order must have at least one item.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId)
                    .GreaterThan(0)
                    .WithMessage("ProductId must be greater than zero.");

                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0)
                    .WithMessage("Quantity must be greater than zero.");
            });

            RuleFor(x => x.PaymentMethod)
                .IsInEnum()
                .WithMessage(localizationService.GetMessage("InvalidPaymentMethod") ?? "Invalid payment method.");

            RuleFor(x => x.OrderType)
                .IsInEnum()
                .WithMessage(localizationService.GetMessage("InvalidOrderType") ?? "Invalid order type.");

            RuleFor(x => x.DiscountPercentage)
                .InclusiveBetween(0, 70)
                .WithMessage("Discount must be between 0% and 70%.");
        }
    }
}
