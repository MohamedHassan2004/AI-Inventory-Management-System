using FluentValidation;
using Inventory.Application.DTOs.ReturnOrder;
using Inventory.Application.Interfaces;
using System;
using System.Linq;

namespace Inventory.Application.Validation.ReturnOrder
{
    public class CreateReturnOrderValidator : AbstractValidator<CreateReturnOrderDto>
    {
        public CreateReturnOrderValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.OriginalOrderId)
                .GreaterThan(0).WithMessage(localizationService.GetMessage("InvalidOrderId"));

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage(localizationService.GetMessage("EmptyReturnOrder"))
                .Must(items => items.Select(i => i.OriginalOrderItemId).Distinct().Count() == items.Count)
                .WithMessage(localizationService.GetMessage("DuplicateReturnItem"));

            RuleForEach(x => x.Items).ChildRules(items =>
            {
                items.RuleFor(i => i.OriginalOrderItemId)
                    .GreaterThan(0).WithMessage(localizationService.GetMessage("InvalidOrderItemId"));

                items.RuleFor(i => i.Quantity)
                    .GreaterThan(0).WithMessage(localizationService.GetMessage("QuantityMustBeGreaterThanZero"));

                items.RuleFor(i => i.NewExpiryDate)
                    .Must(date => date > DateTime.UtcNow).WithMessage(localizationService.GetMessage("ExpiryDateMustBeFuture"));
            });
        }
    }
}
