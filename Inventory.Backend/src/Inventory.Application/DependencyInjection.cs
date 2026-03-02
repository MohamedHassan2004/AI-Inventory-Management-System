using System.Reflection;
using FluentValidation;
using Inventory.Application.Interfaces;
using Inventory.Application.Interfaces.Auth;
using Inventory.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICategoryService, CategoryService>();







        return services;
    }
}
