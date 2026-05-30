using FluentValidation;
using Inventory.Application.DTOs.Order;
using Inventory.Application.Interfaces;

namespace Inventory.Application.Validation.Order;

public class ConfirmOrderValidator : AbstractValidator<ConfirmOrderDto>
{
    public ConfirmOrderValidator(ILocalizationService localizationService)
    {
        RuleFor(x => x.PaymentMethod)
            .IsInEnum()
            .WithMessage(localizationService.GetMessage("InvalidPaymentMethod") ?? "Invalid payment method.");

        RuleFor(x => x.OrderType)
            .IsInEnum()
            .WithMessage(localizationService.GetMessage("InvalidOrderType") ?? "Invalid order type.");

        RuleFor(x => x.DiscountPercentage)
            .InclusiveBetween(0, 70)
            .When(x => x.DiscountPercentage.HasValue)
            .WithMessage("Discount must be between 0% and 70%.");
    }
}
