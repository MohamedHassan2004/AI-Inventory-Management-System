using FluentValidation;
using Inventory.Application.DTOs.Auth;
using Inventory.Application.Interfaces;

namespace Inventory.Application.Validation.Auth
{
    public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
    {
        public ChangePasswordDtoValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage(localizationService.GetMessage("CurrentPasswordRequired"));

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage(localizationService.GetMessage("NewPasswordRequired"))
                .MinimumLength(8).WithMessage(localizationService.GetMessage("NewPasswordMinLength"))
                .MaximumLength(100).WithMessage(localizationService.GetMessage("NewPasswordMaxLength"))
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$")
                    .WithMessage(localizationService.GetMessage("PasswordComplexity"))
                .NotEqual(x => x.CurrentPassword).WithMessage(localizationService.GetMessage("NewPasswordSameAsCurrent"));

            RuleFor(x => x.ConfirmNewPassword)
                .Equal(x => x.NewPassword).WithMessage(localizationService.GetMessage("ConfirmPasswordMismatch"));
        }
    }
}
