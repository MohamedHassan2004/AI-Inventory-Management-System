using FluentValidation;
using Inventory.Application.DTOs.Supplier;

namespace Inventory.Application.Validation.Supplier
{
    public class UpdateSupplierDtoValidator : AbstractValidator<UpdateSupplierDto>
    {
        public UpdateSupplierDtoValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Valid Id is required.");
                
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.");
        }
    }
}