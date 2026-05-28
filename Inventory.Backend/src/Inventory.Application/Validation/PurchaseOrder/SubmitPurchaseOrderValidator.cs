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
                    .WithMessage("ExpiryDate is required.")
                    .GreaterThan(DateTime.UtcNow.AddDays(30))
                    .WithMessage("ExpiryDate must be at least 30 days from current date.");

                item.RuleFor(i => i.DiscountPercentage)
                    .InclusiveBetween(0, 100)
                    .WithMessage("Discount percentage must be between 0 and 100.");
            });
        }
    }
}
