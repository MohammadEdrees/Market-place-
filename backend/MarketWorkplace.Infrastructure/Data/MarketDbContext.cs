using MarketWorkplace.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketWorkplace.Infrastructure.Data;

/// <summary>
/// Entity Framework Core context for the dashboard, mapped Code First to SQL Server
/// (<c>UseSqlServer</c> with the <c>ConnectionStrings:MarketDb</c> setting). The schema is
/// created by the migrations under <c>backend/Migrations</c> (<c>Database.Migrate()</c> in
/// <c>DbInitializer</c>); add a new migration whenever an entity changes.
/// </summary>
/// <remarks>Registered scoped: <c>builder.Services.AddDbContext&lt;MarketDbContext&gt;(…)</c>.</remarks>
public class MarketDbContext(DbContextOptions<MarketDbContext> options) : DbContext(options)
{
    /// <summary>Catalogue items shown on the Products page.</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>Orders powering the dashboard metrics and charts.</summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>Services offered by providers/sellers.</summary>
    public DbSet<Service> Services => Set<Service>();

    /// <summary>Dashboard users; passwords stored as PBKDF2 hashes.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Gallery images attached to products and services (files live in wwwroot).</summary>
    public DbSet<ListingImage> Images => Set<ListingImage>();

    /// <summary>Roles assigned to users; managed by the roles CRUD.</summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>Managed category names for product and service listings; managed by the categories CRUD.</summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>Advertisement slides powering the mobile home slider.</summary>
    public DbSet<Advertisement> Advertisements => Set<Advertisement>();

    /// <summary>Subscription plans purchased by accounts (mobile users primarily).</summary>
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            // Keys are assigned by the app (seed + Max+1 in the controllers), never by the store.
            entity.Property(p => p.Id).ValueGeneratedNever();
            entity.Property(p => p.Name).IsRequired().HasMaxLength(120);
            entity.Property(p => p.Sku).IsRequired().HasMaxLength(40);
            entity.Property(p => p.Category).IsRequired().HasMaxLength(60);
            entity.Property(p => p.Price).HasPrecision(18, 2);
            entity.HasIndex(p => p.SellerId);
            // Status is a derived display value — computed, never stored.
            entity.Ignore(p => p.Status);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Id).ValueGeneratedNever();
            entity.Property(o => o.Customer).IsRequired().HasMaxLength(200);
            entity.Property(o => o.Product).IsRequired().HasMaxLength(120);
            entity.Property(o => o.Category).IsRequired().HasMaxLength(60);
            entity.Property(o => o.Total).HasPrecision(18, 2);
            entity.Property(o => o.Status).IsRequired().HasMaxLength(40);
            entity.HasIndex(o => o.ProductId);
            entity.HasIndex(o => o.ServiceId);
            entity.HasIndex(o => o.BuyerId);
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).ValueGeneratedNever();
            entity.Property(s => s.Title).IsRequired().HasMaxLength(120);
            entity.Property(s => s.Description).HasMaxLength(1000);
            entity.Property(s => s.Category).IsRequired().HasMaxLength(60);
            entity.Property(s => s.Cost).HasPrecision(18, 2);
            entity.Property(s => s.ContactInfo).IsRequired().HasMaxLength(200);
            entity.Property(s => s.Location).IsRequired().HasMaxLength(120);
            entity.Property(s => s.Offers).HasMaxLength(500);
            entity.HasIndex(s => s.ProviderId);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).ValueGeneratedNever();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
            entity.Property(u => u.Name).IsRequired().HasMaxLength(120);
            entity.Property(u => u.PasswordHash).IsRequired();
            // New accounts default to "now" at the store, and rows that predate the
            // column receive the migration-time timestamp (dashboard "new users" stat).
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).ValueGeneratedNever();
            entity.Property(r => r.Name).IsRequired().HasMaxLength(40);
            entity.Property(r => r.Description).HasMaxLength(300);
            entity.HasIndex(r => r.Name).IsUnique();
        });

        modelBuilder.Entity<ListingImage>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Id).ValueGeneratedNever();
            entity.Property(i => i.Path).IsRequired().HasMaxLength(300);
            entity.HasIndex(i => i.ProductId);
            entity.HasIndex(i => i.ServiceId);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).ValueGeneratedNever();
            entity.Property(c => c.Name).IsRequired().HasMaxLength(60);
            entity.Property(c => c.Kind).IsRequired().HasMaxLength(20);
            // "Audio" may exist as both a product and a service category — uniqueness is per kind.
            entity.HasIndex(c => new { c.Name, c.Kind }).IsUnique();
        });

        modelBuilder.Entity<Advertisement>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id).ValueGeneratedNever();
            entity.Property(a => a.Title).IsRequired().HasMaxLength(120);
            entity.Property(a => a.Subtitle).HasMaxLength(240);
            entity.Property(a => a.ImagePath).HasMaxLength(300);
            entity.Property(a => a.TargetUrl).HasMaxLength(300);
            entity.HasIndex(a => a.IsActive);
            // ActiveNow is a derived schedule check — computed, never stored.
            entity.Ignore(a => a.ActiveNow);
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).ValueGeneratedNever();
            entity.Property(s => s.Plan).IsRequired().HasMaxLength(30);
            entity.Property(s => s.BillingCycle).IsRequired().HasMaxLength(10);
            entity.Property(s => s.Status).IsRequired().HasMaxLength(10);
            entity.Property(s => s.Price).HasPrecision(18, 2);
            entity.HasIndex(s => s.UserId);
        });

        // --- Relationships (products related to their provider, orders to buyers/listings) ---

        // A subscription belongs to one account; deleting a user keeps their
        // history, so the subscriber reference is left as-is (SetNull is not
        // meaningful here — the subscription is a record about the user).
        modelBuilder.Entity<Subscription>()
            .HasOne(s => s.User)
            .WithMany(u => u.Subscriptions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Gallery images hang off one listing each; controllers delete the files explicitly,
        // the cascade only keeps the table clean if a listing ever disappears another way.
        modelBuilder.Entity<Product>()
            .HasMany(p => p.Images)
            .WithOne()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Service>()
            .HasMany(s => s.Images)
            .WithOne()
            .HasForeignKey(i => i.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
