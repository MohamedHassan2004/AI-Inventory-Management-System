using Inventory.API.Middlewares;
using Inventory.Application;
using Inventory.Domain.Entities.Users;
using Inventory.Infrastructure;
using Inventory.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Serilog;
using Serilog.Context;
using System.Text.Json.Serialization;

namespace Inventory.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Create Serilog Logger
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .CreateLogger();

            try
            {
                Log.Information("Starting web application");
                builder.Host.UseSerilog();
                builder.Logging.AddConsole();

                // Add services to the container.
                builder.Services.AddControllers()
                    .AddJsonOptions(options =>
                    {
                        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    });
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();

                // Add Application and Infrastructure services
                builder.Services.AddApplication();
                builder.Services.AddInfrastructure(builder.Configuration);

                var app = builder.Build();

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                // seed roles and super admin user
                using (var scope = app.Services.CreateScope())
                {
                    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                    await ApplicationDbContextSeed.SeedRolesAsync(roleManager);

                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    await ApplicationDbContextSeed.SeedAdminUserAsync(userManager);
                }

                app.UseMiddleware<GlobalErrorHandlingMiddleware>();

                app.Use(async (context, next) =>
                {
                    using (LogContext.PushProperty("RequestId", context.TraceIdentifier))
                    using (LogContext.PushProperty("RequestPath", context.Request.Path))
                    {
                        await next();
                    }
                });

                app.UseHttpsRedirection();

                app.UseAuthentication();
                app.UseAuthorization();

                app.MapControllers();

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}