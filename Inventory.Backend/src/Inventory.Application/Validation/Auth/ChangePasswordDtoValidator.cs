using FluentValidation;
using Inventory.Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Validation.Auth
{
    public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
    {
        public ChangePasswordDtoValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("Current password is required.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("New password is required.")
                .MinimumLength(8).WithMessage("New password must be at least 8 characters long.")
                .MaximumLength(100).WithMessage("New password must not exceed 100 characters.")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$")
                    .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.")
                .NotEqual(x => x.CurrentPassword).WithMessage("New password cannot be the same as the current password.");

            RuleFor(x => x.ConfirmNewPassword)
                .Equal(x => x.NewPassword).WithMessage("Confirm new password must match the new password.");
        }
    }
}
