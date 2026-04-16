using Microsoft.OpenApi;

namespace Inventory.API.Extensions;

public static class OpenApiExtension
{
  public static IServiceCollection AddOpenApiExtension(this IServiceCollection services)
  {
    services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            if (document.Info is not null)
            {
                document.Info.Title = "Inventory Management API";
                document.Info.Version = "v1";
                document.Info.Description = "API for managing warehouse inventory, products, and orders.";
            }

            var scheme = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Name = "Authorization",
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token in the format: Bearer {token}"
            };

            var components = document.Components ??= new OpenApiComponents();
            components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            components.SecuritySchemes["Bearer"] = scheme;

            var security = document.Security ??= new List<OpenApiSecurityRequirement>();
            security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
            });

            return Task.CompletedTask;
        });
    });

    return services;
  }
}