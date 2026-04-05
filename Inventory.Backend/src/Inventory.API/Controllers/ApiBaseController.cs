using Inventory.Application.Interfaces;
using Inventory.Domain.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers
{
    [ApiController]
    public class ApiBaseController : ControllerBase
    {
        protected IActionResult HandleResult(Result result)
        {
            if (result.IsSuccess)
            {
                return NoContent();
            }

            return CreateProblemDetails(result.Error);
        }

        protected IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return CreateProblemDetails(result.Error);
        }

        private IActionResult CreateProblemDetails(Error error)
        {
            var localizationService = HttpContext.RequestServices.GetRequiredService<ILocalizationService>();

            var statusCode = error.Type switch
            {
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status500InternalServerError
            };

            var typeKey = error.Type switch
            {
                ErrorType.NotFound => "ResourceNotFound",
                ErrorType.Validation => "BadRequest",
                ErrorType.Conflict => "ResourceConflict",
                ErrorType.Unauthorized => "UnauthorizedAccess",
                _ => "InternalServerError"
            };

            return Problem(
                statusCode : statusCode,
                type: localizationService.GetMessage(typeKey),
                title: error.Code,
                detail: error.Description
                );
        }
    }
}
