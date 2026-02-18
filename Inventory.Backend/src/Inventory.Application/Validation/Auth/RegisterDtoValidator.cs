using FluentValidation;
using Inventory.Application.DTOs.Auth;
using Inventory.Application.Interfaces;
using Inventory.Domain.Enums;

namespace Inventory.Application.Validation.Auth
{
    public class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        public RegisterDtoValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage(localizationService.GetMessage("UsernameRequired"))
                .Matches(@"^[a-zA-Z0-9_]+$").WithMessage(localizationService.GetMessage("UsernameAlphanumeric"))
                .MaximumLength(50).WithMessage(localizationService.GetMessage("UsernameMaxLength"));

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage(localizationService.GetMessage("FullNameRequired"))
                .Matches(@"^[a-zA-Z0-9_]+$").WithMessage(localizationService.GetMessage("UsernameAlphanumeric"))
                .MaximumLength(50).WithMessage(localizationService.GetMessage("FullNameMaxLength"));

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(localizationService.GetMessage("EmailRequired"))
                .EmailAddress().WithMessage(localizationService.GetMessage("EmailInvalid"))
                .MaximumLength(100).WithMessage(localizationService.GetMessage("EmailMaxLength"));

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage(localizationService.GetMessage("PhoneRequired"))
                .Matches(@"^\d{11}$").WithMessage(localizationService.GetMessage("PhoneExactDigits"));

            RuleFor(x => x.Roles)
            .NotNull().WithMessage(localizationService.GetMessage("RolesRequired"))
            .Must(roles => roles != null && roles.Count > 0).WithMessage(localizationService.GetMessage("AtLeastOneRole"))
            .Must(roles => roles.Distinct().Count() == roles.Count).WithMessage(localizationService.GetMessage("NoDuplicateRoles"));

            RuleForEach(x => x.Roles)
                .IsInEnum().WithMessage((dto, role) => localizationService.GetMessage("InvalidRoleValue", role));

        }
    }
}
