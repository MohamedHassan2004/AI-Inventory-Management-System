using FluentValidation;
using Inventory.Application.DTOs.Auth;
using Inventory.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Validation.Auth
{
    public class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        public RegisterDtoValidator()
        {
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("Username is required.")
                .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Username can only contain alphanumeric characters and underscores.")
                .MaximumLength(50).WithMessage("Username must not exceed 50 characters.");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full name is required.")
                .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Username can only contain alphanumeric characters and underscores.")
                .MaximumLength(50).WithMessage("Full name must not exceed 50 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email is required.")
                .MaximumLength(100).WithMessage("Email must not exceed 100 characters.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Matches(@"^\d{11}$").WithMessage("Phone number must be exactly 11 digits.");

            RuleFor(x => x.Roles)
            .NotNull().WithMessage("Roles Cannot be empty.")
            .Must(roles => roles != null && roles.Count > 0).WithMessage("User must have at least one role.")
            .Must(roles => roles.Distinct().Count() == roles.Count).WithMessage("Cannot repeat the same role.");

            RuleForEach(x => x.Roles)
                .IsInEnum().WithMessage((dto, role) => $"This Value ({role}) is not allowed.");

        }
    }
}
