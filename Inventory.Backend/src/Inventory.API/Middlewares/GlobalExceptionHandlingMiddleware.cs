using Inventory.Application.Interfaces;
using Inventory.Domain.Shared;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace Inventory.API.Middlewares;

public class GlobalErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalErrorHandlingMiddleware> _logger;

    public GlobalErrorHandlingMiddleware(RequestDelegate next, ILogger<GlobalErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ILocalizationService localizationService)
    {
        try
        {
            await _next(context);
            await HandelNotFoundEndpointAsync(context, localizationService);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Something Went Wrong");

            var problem = new ProblemDetails
            {
                Title = localizationService.GetMessage("GlobalErrorTitle"),
                Detail = ex.Message,
                Status = StatusCodes.Status500InternalServerError,
                Instance = context.Request.Path
            };
            context.Response.StatusCode = problem.Status.Value;
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
    
    private static async Task HandelNotFoundEndpointAsync(HttpContext httpContext, ILocalizationService localizationService)
    {
        if (httpContext.Response.StatusCode == StatusCodes.Status404NotFound && !httpContext.Response.HasStarted)
        {
            var problem = new ProblemDetails
            {
                Title = localizationService.GetMessage("EndpointNotFoundTitle"),
                Detail = localizationService.GetMessage("EndpointNotFoundDetail", httpContext.Request.Path),
                Status = StatusCodes.Status404NotFound,
                Instance = httpContext.Request.Path
            };
            await httpContext.Response.WriteAsJsonAsync(problem);
        }
    }
}