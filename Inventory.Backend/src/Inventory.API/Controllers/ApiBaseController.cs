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
            var statusCode = error.Type switch
            {
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status500InternalServerError
            };

            return Problem(
                statusCode: statusCode,
                title: GetTitleForErrorType(error.Type),
                detail: error.Description,
                extensions: new Dictionary<string, object?>
                {
                    { "customErrorCode", error.Code }
                });
        }

        private static string GetTitleForErrorType(ErrorType errorType) =>
            errorType switch
            {
                ErrorType.NotFound => "Resource Not Found",
                ErrorType.Validation => "Bad Request",
                ErrorType.Conflict => "Resource Conflict",
                ErrorType.Unauthorized => "Unauthorized Access",
                _ => "Internal Server Error"
            };
    }
}
