using MarketWorkplace.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MarketWorkplace.Api.Data;

/// <summary>
/// Entity Framework Core context for the dashboard, backed by the in-memory provider
/// (<c>UseInMemoryDatabase</c>) so the API runs without a database server while keeping
/// the full <c>DbContext</c>/<c>DbSet</c> pipeline. Swapping to SQL Server later is a
/// one-line change in <c>Program.cs</c> (add <c>Microsoft.EntityFrameworkCore.SqlServer</c>
/// and call <c>UseSqlServer(connectionString)</c> instead).
/// </summary>
/// <remarks>Registered scoped: <c>builder.Services.AddDbContext&lt;MarketDbContext&gt;(…)</c>.</remarks>
public class MarketDbContext(DbContextOptions<MarketDbContext> options) : DbContext(options)
{
    /// <summary>Catalogue items shown on the Products page.</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>Orders powering the dashboard metrics and charts.</summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>Dashboard users; passwords stored as PBKDF2 hashes.</summary>
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(120);
            entity.Property(p => p.Sku).IsRequired().HasMaxLength(40);
            entity.Property(p => p.Category).IsRequired().HasMaxLength(60);
            // Status is a derived display value — computed, never stored.
            entity.Ignore(p => p.Status);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Customer).IsRequired().HasMaxLength(200);
            entity.Property(o => o.Product).IsRequired().HasMaxLength(120);
            entity.Property(o => o.Category).IsRequired().HasMaxLength(60);
            entity.Property(o => o.Status).IsRequired().HasMaxLength(40);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
            entity.Property(u => u.Name).IsRequired().HasMaxLength(120);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).IsRequired().HasMaxLength(60);
            entity.HasIndex(u => u.Email).IsUnique();
        });
    }
}
