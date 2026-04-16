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
                .MaximumLength(50).WithMessage("Username must not exceed 50 characters.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .Length(8, 100).WithMessage("Password must be between 8 and 100 characters.");
        }
    }
}
