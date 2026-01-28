using FluentValidation;
using Inventory.Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Validation.Auth
{
    public class LoginDtoValiditor : AbstractValidator<LoginDto>
    {
        public LoginDtoValiditor()
        {
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("Username is required.")
                .Matches("^[a-zA-Z0-9_]+$").WithMessage("Username can only contain letters, numbers, and underscores.")
                .MaximumLength(50).WithMessage("Username must not exceed 50 characters.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$")
                    .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.")
                .Length(8, 100).WithMessage("Password must be between 8 and 100 characters.");
        }
    }
}
