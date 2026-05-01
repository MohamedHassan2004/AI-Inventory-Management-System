using FluentValidation;
using Inventory.Application.DTOs.PurchaseOrder;

namespace Inventory.Application.Validation.PurchaseOrder
{
    public class SubmitPurchaseOrderValidator : AbstractValidator<SubmitPurchaseOrderDto>
    {
        public SubmitPurchaseOrderValidator()
        {
            RuleFor(x => x.SupplierId)
                .GreaterThan(0)
                .WithMessage("SupplierId must be greater than zero.");

            RuleFor(x => x.Items)
                .NotEmpty()
                .WithMessage("Purchase order must contain at least one item.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId)
                    .GreaterThan(0)
                    .WithMessage("ProductId must be greater than zero.");

                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0)
                    .WithMessage("Quantity must be greater than zero.");

                item.RuleFor(i => i.UnitCost)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("UnitCost cannot be negative.");

                item.RuleFor(i => i.ExpiryDate)
                    .NotEmpty()
                    .WithMessage("ExpiryDate is required.");
            });
        }
    }
}
