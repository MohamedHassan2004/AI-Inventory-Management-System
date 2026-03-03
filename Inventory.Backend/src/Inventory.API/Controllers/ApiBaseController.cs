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

            return CreateProblemDetails(result.ErrorCode, result.Message);
        }

        protected IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return CreateProblemDetails(result.ErrorCode, result.Message);
        }

        private IActionResult CreateProblemDetails(string errorCode, string message)
        {
            var statusCode = GetStatusCodeFromErrorCode(errorCode);

            return Problem(
                statusCode: statusCode,
                title: GetTitleForStatusCode(statusCode),
                detail: message,
                extensions: new Dictionary<string, object?>
                {
                { "customErrorCode", errorCode }
                });
        }

        private static int GetStatusCodeFromErrorCode(string errorCode) =>
            errorCode switch
            {
                var c when c.Contains("NOT_FOUND") || c.StartsWith("NO_") => StatusCodes.Status404NotFound,
                var c when c.Contains("UNAUTHORIZED") || c.Contains("CREDENTIALS") => StatusCodes.Status401Unauthorized,
                var c when c.Contains("DUPLICATE") || c.Contains("ALREADY_EXIST") => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };

        private static string GetTitleForStatusCode(int statusCode) =>
            statusCode switch
            {
                StatusCodes.Status404NotFound => "Resource Not Found",
                StatusCodes.Status401Unauthorized => "Unauthorized Access",
                StatusCodes.Status409Conflict => "Resource Conflict",
                _ => "Bad Request"
            };
    }
}
