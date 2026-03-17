using FluentValidation;
using Inventory.Application.DTOs.Supplier;

namespace Inventory.Application.Validation.Supplier
{
    public class AddSupplierRatingDtoValidator : AbstractValidator<AddSupplierRatingDto>
    {
        public AddSupplierRatingDtoValidator()
        {
            RuleFor(x => x.Rating)
                .InclusiveBetween(0, 5).WithMessage("Rating must be between 0 and 5.");
            
            RuleFor(x => x.Note)
                .MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.Note))
                .WithMessage("Note must not exceed 1000 characters.");
        }
    }
}
