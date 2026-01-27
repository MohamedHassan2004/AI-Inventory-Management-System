using Inventory.API.Middlewares;
using Inventory.Application;
using Inventory.Domain.Entities.Users;
using Inventory.Infrastructure;
using Inventory.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Serilog;
using Serilog.Context;

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

                // Add services to the container.
                builder.Services.AddControllers();
                builder.Services.AddOpenApi();

                // Add Application and Infrastructure services
                builder.Services.AddApplication();
                builder.Services.AddInfrastructure(builder.Configuration);

                var app = builder.Build();

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.MapOpenApi();
                }

                // seed roles and super admin user
                using (var scope = app.Services.CreateScope())
                {
                    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                    await ApplicationDbContextSeed.SeedRolesAsync(roleManager);

                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    await ApplicationDbContextSeed.SeedSuperAdminUserAsync(userManager);
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