using Inventory.Application.Interfaces;
using Inventory.Application.Interfaces.Auth;
using Inventory.Application.Services;
using Inventory.Domain.Entities.Users;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Settings;
using Inventory.Infrastructure.Data;
using Inventory.Infrastructure.Repositories;
using Inventory.Infrastructure.Services.Auth;
using Inventory.Infrastructure.Services.FileService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Configure Identity
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 6;
            options.User.RequireUniqueEmail = true;
            options.Lockout.MaxFailedAccessAttempts = 3;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(2);
        })
            .AddEntityFrameworkStores<ApplicationDbContext>();

        // Configure JWT Settings
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;

        // Configure JWT Authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    //ValidateIssuer = true,
                    //ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    //ValidIssuer = jwtSettings.Issuer,
                    //ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                    RoleClaimType = ClaimTypes.Role
                };
            });

        // Register repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<IDateTimeProvider, Inventory.Infrastructure.Services.DateTimeProvider>();

        // Configure and register FileService
        services.Configure<FileServiceOptions>(
            configuration.GetSection(FileServiceOptions.SectionName));
        services.AddScoped<IFileService, FileService>();

        // Register RoleService and UserAdminService
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICategoryRepository,CategoryRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IStockBatchRepository, StockBatchRepository>();

        // Register Localization
        services.AddScoped<ILocalizationService, Inventory.Infrastructure.Services.LocalizationService>();

        return services;
    }
}
