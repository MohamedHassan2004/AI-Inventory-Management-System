using Inventory.Application.Interfaces;
using Inventory.Application.Interfaces.Auth;
using Inventory.Application.Interfaces.Documents;
using Inventory.Application.Interfaces.Queries;
using Inventory.Application.Interfaces.Queries.Reports;
using Inventory.Domain.Entities.Users;
using Inventory.Domain.Interfaces;
using Inventory.Infrastructure.BackgroundServices;
using Inventory.Infrastructure.Data;
using Inventory.Infrastructure.Data.Settings;
using Inventory.Infrastructure.Documents;
using Inventory.Infrastructure.Queries;
using Inventory.Infrastructure.Queries.Reports;
using Inventory.Infrastructure.Repositories;
using Inventory.Infrastructure.Services;
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
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

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
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;

        services.Configure<SystemSettings>(configuration.GetSection(SystemSettings.SectionName));

        
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });


        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        
        services.Configure<FileServiceOptions>(configuration.GetSection(FileServiceOptions.SectionName));
        services.AddScoped<IFileService, FileService>();

        
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICategoryRepository,CategoryRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IStockBatchRepository, StockBatchRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IReturnOrderRepository, ReturnOrderRepository>();
        services.AddScoped<IOrderQueryService, OrderQueryService>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<IPurchaseOrderQueryService, PurchaseOrderQueryService>();
        services.AddScoped<IReturnOrderQueryService, ReturnOrderQueryService>();
        services.AddScoped<ISalesReportQuery, SalesReportQuery>();
        services.AddScoped<IInventoryReportQuery, InventoryReportQuery>();
        services.AddScoped<IReturnsReportQuery, ReturnsReportQuery>();
        services.AddScoped<ISupplierReportQuery, SupplierReportQuery>();
        services.AddScoped<IUsersReportQuery, UsersReportQuery>();
        services.AddScoped<IDashboardQuery, DashboardQuery>();
        services.AddScoped<IReceiptService, ReceiptService>();
        services.AddScoped<IPurchaseInvoiceService, PurchaseInvoiceService>();

        services.AddHostedService<DraftOrderCleanupService>();
        services.AddHostedService<ExpiredAllocationCleanupBackgroundService>();

        return services;
    }
}
