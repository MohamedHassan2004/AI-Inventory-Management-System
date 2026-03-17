using Inventory.Application.Interfaces;
using Inventory.Domain.Consts;
using Inventory.Domain.Shared;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace Inventory.API.Extensions;

public static class RateLimiterExtension
{
  public static IServiceCollection AddRateLimiting(this IServiceCollection services)
  {
    services.AddRateLimiter(options =>
    {
      options.RejectionStatusCode = 429;

      options.OnRejected = async (context, cancellationToken) =>
          {
          var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
          logger.LogWarning("Rate limit exceeded for IP: {IpAddress}", context.HttpContext.Connection.RemoteIpAddress);

          var response = context.HttpContext.Response;
          response.StatusCode = 429;
          response.ContentType = "application/json";

          var localizationService = context.HttpContext.RequestServices.GetService<ILocalizationService>();
          var message = localizationService?.GetMessage("RateLimitExceeded") ?? "Too many requests. Please try again later.";

          var result = new Error("RateLimiter", message, ErrorType.Failure);

          var jsonOptions = context.HttpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

          await response.WriteAsync(JsonSerializer.Serialize(result, jsonOptions));
        };

      options.AddPolicy(RateLimiterPolicies.TokenBucket, context =>
          {
          var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "global";
          return RateLimitPartition.GetTokenBucketLimiter(
                  partitionKey: ipAddress,
                  factory: _ => new TokenBucketRateLimiterOptions
                {
                  TokenLimit = 100,
                  ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                  TokensPerPeriod = 10,
                  AutoReplenishment = true,
                  QueueLimit = 0
                });
        });

      options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
          {
          return RateLimitPartition.GetFixedWindowLimiter(
                  partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "global",
                  factory: _ => new FixedWindowRateLimiterOptions
                {
                  Window = TimeSpan.FromMinutes(1),
                  PermitLimit = 100,
                  QueueLimit = 0
                });
        });
    });

    return services;
  }
}