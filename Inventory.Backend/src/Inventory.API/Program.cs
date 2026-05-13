using Inventory.API.Extensions;
using Inventory.API.Filter;
using Inventory.API.Filter.Handlers;
using Inventory.API.Filter.Requirements;
using Inventory.API.Middleware;
using Inventory.API.Middlewares;
using Inventory.API.Settings;
using Inventory.Application;
using Inventory.Application.Mappings;
using Inventory.Domain.Entities.Users;
using Inventory.Domain.Enums;
using Inventory.Infrastructure;
using Inventory.Infrastructure.Data;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Serilog;
using Serilog.Context;
using System.Globalization;
using System.Text.Json.Serialization;
using Scalar.AspNetCore;
using Microsoft.OpenApi;
using QuestPDF.Infrastructure;


namespace Inventory.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            QuestPDF.Settings.License = LicenseType.Community;

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
                builder.Services.AddScoped<ValidationFilter>();

                // Configure CORS — must allow Authorization header so JWT tokens reach the server
                var allowedOrigins = builder.Configuration
                    .GetSection("Cors:AllowedOrigins")
                    .Get<string[]>() ?? Array.Empty<string>();

                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowFrontend", policy =>
                    {
                        policy.WithOrigins(allowedOrigins)
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    });
                });

                builder.Services.AddRateLimiting();

                builder.Services.AddControllers(options => 
                    options.Filters.Add<ValidationFilter>())
                    .AddJsonOptions(options =>
                    {
                        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

                    })
                    .AddDataAnnotationsLocalization();

                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddOpenApiExtension();

                builder.Services.AddHealthChecks()
                    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!);

                // Add Application and Infrastructure services
                builder.Services.AddApplication();
                builder.Services.AddInfrastructure(builder.Configuration);
                builder.Services.Configure<RefreshTokenCookieSettings>(
                    builder.Configuration.GetSection(RefreshTokenCookieSettings.SectionName));

                builder.Services.AddScoped<IAuthorizationHandler, StatusHandler>();

                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy("Active", policy =>
                        policy.Requirements.Add(new StatusRequirement(AccountStatus.Active)));

                    options.AddPolicy("UploadIdentity", policy =>
                        policy.Requirements.Add(new StatusRequirement(
                            AccountStatus.PendingIdentityUpload,
                            AccountStatus.Active,
                            AccountStatus.Rejected)));
                });

                // Configure Request Localization
                builder.Services.Configure<RequestLocalizationOptions>(options =>
                {
                    var supportedCultures = new[]
                    {
                        new CultureInfo("en"),
                        new CultureInfo("ar")
                    };

                    options.DefaultRequestCulture = new RequestCulture("en");
                    options.SupportedCultures = supportedCultures;
                    options.SupportedUICultures = supportedCultures;

                    options.RequestCultureProviders = new List<IRequestCultureProvider>
                    {
                        new AcceptLanguageHeaderRequestCultureProvider(),
                        new QueryStringRequestCultureProvider(),
                        new CookieRequestCultureProvider()
                    };
                });

                var app = builder.Build();

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.MapOpenApi();
                    app.MapScalarApiReference(options => 
                    {
                        options.WithTitle("Inventory API Reference")
                               .WithTheme(ScalarTheme.Moon)
                               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                               .HideModels();
                    });
                }

                // seed roles and super admin user
                using (var scope = app.Services.CreateScope())
                {
                    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                    await ApplicationDbContextSeed.SeedRolesAsync(roleManager);

                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    await ApplicationDbContextSeed.SeedAdminUserAsync(userManager);

                    // Seed domain entities securely using the orchestrated seeder
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    await Inventory.Infrastructure.Data.Seeders.DatabaseSeeder.SeedAllAsync(context, userManager);
                }

                app.UseRequestLocalization();

                app.UseMiddleware<GlobalErrorHandlingMiddleware>();

                app.UseHttpsRedirection();

                app.UseCors("AllowFrontend");

                app.UseStaticFiles();

                app.UseRateLimiter();
                app.UseRateLimitHeaders();

                app.UseAuthentication();
                app.UseAuthorization();

                app.MapHealthChecks("/health");
                app.MapControllers();

                app.Run();
            }
            catch (Exception ex)
            {
                if (ex is Microsoft.Extensions.Hosting.HostAbortedException)
                {
                    Log.Information("Host aborted successfully (e.g., by EF Core tools).");
                }
                else
                {
                    Log.Fatal(ex, "Application terminated unexpectedly");
                }
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
