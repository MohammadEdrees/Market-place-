using MarketWorkplace.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MarketWorkplace.Infrastructure.Data;

/// <summary>
/// DI wiring for the data tier. Keeps EF Core (SQL Server provider, connection string
/// <c>ConnectionStrings:MarketDb</c>) and the repositories behind the interfaces the
/// Application layer consumes, so the API project never references EF directly.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the <see cref="MarketDbContext"/> and the repository layer.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MarketDb")
            ?? throw new InvalidOperationException("ConnectionStrings:MarketDb is missing from configuration.");
        services.AddDbContext<MarketDbContext>(options => options.UseSqlServer(connectionString));

        // Repositories: generic over any entity + dedicated ones with eager-load queries.
        // All of them share the request's scoped MarketDbContext, so one SaveChanges commits
        // every pending change of the unit of work.
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();

        // Image file storage (wwwroot); IImageStore is what the application services consume.
        services.AddTransient<ImageStore>();
        services.AddTransient<IImageStore>(sp => sp.GetRequiredService<ImageStore>());

        return services;
    }
}
