using FluentValidation;
using Inventory.Application.Interfaces;
using Inventory.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Inventory.API.Filter
{
    public class ValidationFilter : IAsyncActionFilter
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILocalizationService _localization;

        public ValidationFilter(
            IServiceProvider serviceProvider,
            ILocalizationService localization)
        {
            _serviceProvider = serviceProvider;
            _localization = localization;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var errors = new List<object>();

            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument == null) continue;

                var argumentType = argument.GetType();

                if (argumentType.IsPrimitive || argumentType == typeof(string))
                    continue;

                var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
                var validator = _serviceProvider.GetService(validatorType) as IValidator;

                if (validator == null) continue;

                var validationContext = new ValidationContext<object>(argument);
                var result = await validator.ValidateAsync(validationContext);

                if (!result.IsValid)
                {
                    errors.AddRange(result.Errors.Select(e => new
                    {
                        Field = e.PropertyName,
                        Message = e.ErrorMessage
                    }));
                }
            }

            if (errors.Any())
            {
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Type = _localization.GetMessage("BadRequest"),
                    Title = _localization.GetMessage("ValidationErrorTitle"),
                    Detail = "See errors for more details"
                };

                context.Result = new BadRequestObjectResult(new
                {
                    problemDetails.Title,
                    problemDetails.Status,
                    problemDetails.Type,
                    Errors = errors
                });

                return;
            }

            await next();
        }
    }
}
