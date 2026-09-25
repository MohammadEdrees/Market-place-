using MarketWorkplace.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MarketWorkplace.Application;

/// <summary>DI wiring for the business layer: the application services the controllers delegate to.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers every application service (scoped, one unit of work per request).</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<UsersService>();
        services.AddScoped<RolesService>();
        services.AddScoped<ProductsService>();
        services.AddScoped<ServicesService>();
        services.AddScoped<OrdersService>();
        services.AddScoped<DashboardService>();
        return services;
    }
}
