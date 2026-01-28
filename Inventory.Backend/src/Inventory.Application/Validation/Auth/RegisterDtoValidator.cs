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

            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Role is required.")
                .NotEqual(UserRole.None).WithMessage("Please select a valid role.")
                .IsInEnum().WithMessage("The selected role does not exist.");
        }
    }
}
