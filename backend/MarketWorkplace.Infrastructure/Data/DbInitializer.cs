using MarketWorkplace.Application.Common;
using MarketWorkplace.Application.Security;
using MarketWorkplace.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketWorkplace.Infrastructure.Data;

/// <summary>
/// Creates the schema (EF Core migration — Code First on SQL Server) and loads the sample
/// data (products, orders, users, gallery images). Runs once at startup; each collection is
/// seeded only when it is empty, so restarts keep whatever is already in the database.
/// </summary>
public static class DbInitializer
{
    /// <summary>Applies pending migrations and seeds any missing collections.</summary>
    /// <param name="db">The context to migrate and seed.</param>
    /// <param name="imageStore">Used to prefix seeded image paths with the configured <c>BackendUrl</c>.</param>
    /// <param name="webRoot">Absolute wwwroot path where placeholder gallery/avatar images are written.</param>
    public static void Initialize(MarketDbContext db, ImageStore imageStore, string webRoot)
    {
        // Code First: creates the database on first run and applies any new migrations.
        db.Database.Migrate();

        // Roles must exist before the users that reference them (RoleId foreign key).
        var roles = EnsureRoles(db);

        if (!db.Users.Any())
        {
            db.Users.AddRange(BuildUsers(roles));
        }

        var products = db.Products.OrderBy(p => p.Id).ToList();
        if (products.Count == 0)
        {
            products = BuildProducts();
            db.Products.AddRange(products);
        }

        var services = db.Services.OrderBy(s => s.Id).ToList();
        if (services.Count == 0)
        {
            services = BuildServices();
            db.Services.AddRange(services);
        }

        if (!db.Orders.Any())
        {
            db.Orders.AddRange(BuildOrders(products, services));
        }

        db.SaveChanges();

        // The managed category list must exist before the chips/forms read it: it absorbs
        // every category already typed onto a listing, then the ads can be seeded below.
        EnsureCategories(db);
        EnsureAdvertisements(db, imageStore, webRoot);

        if (!db.Images.Any())
        {
            SeedImages(db, imageStore, webRoot);
        }

        if (db.Users.Any(u => u.ImagePath == null))
        {
            SeedUserAvatars(db, imageStore, webRoot);
        }
    }

    /// <summary>Seeds the built-in roles when the table is empty and returns them keyed by name.</summary>
    private static Dictionary<string, Role> EnsureRoles(MarketDbContext db)
    {
        if (!db.Roles.Any())
        {
            db.Roles.AddRange(
                new Role { Id = 1, Name = "SuperAdmin", Description = "Full control of the platform, including roles and accounts." },
                new Role { Id = 2, Name = "Admin", Description = "Manages every listing, order and account from the dashboard." },
                new Role { Id = 3, Name = "Manager", Description = "Curates listings and orders from the dashboard." },
                new Role { Id = 4, Name = "Viewer", Description = "Read-only dashboard access." },
                new Role { Id = 5, Name = "Provider", Description = "Mobile seller: publishes products and services." },
                new Role { Id = 6, Name = "Client", Description = "Mobile buyer: purchases products and reserves services." });

            // Persist now: BuildUsers links accounts to these rows before the shared SaveChanges below.
            db.SaveChanges();
        }

        return db.Roles.ToDictionary(r => r.Name, r => r, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Adds every category used by a listing that is missing from the managed list.</summary>
    /// <remarks>Runs on every startup: it backfills existing databases after an upgrade and
    /// repairs drift, but never deletes — removal is an explicit dashboard action (and is
    /// blocked there while listings still use the name).</remarks>
    private static void EnsureCategories(MarketDbContext db)
    {
        var known = db.Categories.AsEnumerable()
            .Select(c => $"{c.Kind}|{c.Name.ToLowerInvariant()}")
            .ToHashSet();

        var nextId = db.Categories.Any() ? db.Categories.Max(c => c.Id) + 1 : 1;
        var added = false;

        foreach (var name in db.Products.AsEnumerable().Select(p => p.Category).Where(n => n.Length > 0).Distinct())
        {
            if (known.Add($"{Category.ProductKind}|{name.ToLowerInvariant()}"))
            {
                db.Categories.Add(new Category { Id = nextId++, Name = name, Kind = Category.ProductKind });
                added = true;
            }
        }

        foreach (var name in db.Services.AsEnumerable().Select(s => s.Category).Where(n => n.Length > 0).Distinct())
        {
            if (known.Add($"{Category.ServiceKind}|{name.ToLowerInvariant()}"))
            {
                db.Categories.Add(new Category { Id = nextId++, Name = name, Kind = Category.ServiceKind });
                added = true;
            }
        }

        if (added)
        {
            db.SaveChanges();
        }
    }

    /// <summary>Seeds three promotional slides (with generated banner art) when the table is empty.</summary>
    private static void EnsureAdvertisements(MarketDbContext db, ImageStore store, string webRoot)
    {
        if (db.Advertisements.Any())
        {
            return;
        }

        var adsDir = Path.Combine(webRoot, "images", "ads");
        Directory.CreateDirectory(adsDir);

        var now = DateTime.UtcNow;
        var seed = new (string Title, string Subtitle, string? TargetUrl, int SortOrder)[]
        {
            ("Weekend Audio Sale", "Up to 30% off headsets, earbuds and speakers", "product/1", 1),
            ("Free delivery this month", "On every marketplace order over $50", null, 2),
            ("Book a consulting session", "Free 15-minute intro call for new businesses", "service/6", 3),
        };

        var advertisements = new List<Advertisement>(seed.Length);
        for (var i = 0; i < seed.Length; i++)
        {
            var (title, subtitle, targetUrl, sortOrder) = seed[i];
            var file = $"ad-{i + 1}.png";
            File.WriteAllBytes(
                Path.Combine(adsDir, file),
                PlaceholderPng.Generate(1200, 400, Palette(i * 9 + 5), Palette(i * 9 + 21), i % 3));

            advertisements.Add(new Advertisement
            {
                Id = i + 1,
                Title = title,
                Subtitle = subtitle,
                ImagePath = store.ToUrl($"/images/ads/{file}"),
                TargetUrl = targetUrl,
                IsActive = true,
                SortOrder = sortOrder,
                CreatedAt = now,
            });
        }

        db.Advertisements.AddRange(advertisements);
        db.SaveChanges();
    }

    // --- Gallery placeholders ----------------------------------------------

    /// <summary>Writes two generated PNGs per product and service into wwwroot and links them.</summary>
    private static void SeedImages(MarketDbContext db, ImageStore store, string webRoot)
    {
        var productDir = Path.Combine(webRoot, "images", "products");
        var serviceDir = Path.Combine(webRoot, "images", "services");
        Directory.CreateDirectory(productDir);
        Directory.CreateDirectory(serviceDir);

        var nextId = 1;
        var images = new List<ListingImage>();

        foreach (var product in db.Products.OrderBy(p => p.Id).ToList())
        {
            for (var slot = 1; slot <= 2; slot++)
            {
                var file = $"product-{product.Id}-{slot}.png";
                File.WriteAllBytes(
                    Path.Combine(productDir, file),
                    PlaceholderPng.Generate(
                        480, 360,
                        Palette(product.Id * 2 + slot),
                        Palette(product.Id * 2 + slot + 11),
                        (product.Id + slot) % 3));
                images.Add(new ListingImage
                {
                    Id = nextId++,
                    ProductId = product.Id,
                    Path = store.ToUrl($"/images/products/{file}"),
                    SortOrder = slot - 1,
                });
            }
        }

        foreach (var service in db.Services.OrderBy(s => s.Id).ToList())
        {
            for (var slot = 1; slot <= 2; slot++)
            {
                var file = $"service-{service.Id}-{slot}.png";
                File.WriteAllBytes(
                    Path.Combine(serviceDir, file),
                    PlaceholderPng.Generate(
                        480, 360,
                        Palette(service.Id * 3 + slot),
                        Palette(service.Id * 3 + slot + 5),
                        (service.Id + slot + 1) % 3));
                images.Add(new ListingImage
                {
                    Id = nextId++,
                    ServiceId = service.Id,
                    Path = store.ToUrl($"/images/services/{file}"),
                    SortOrder = slot - 1,
                });
            }
        }

        db.Images.AddRange(images);
        db.SaveChanges();
    }

    /// <summary>Writes one generated avatar per user into <c>wwwroot/images/users</c> and links it.</summary>
    private static void SeedUserAvatars(MarketDbContext db, ImageStore store, string webRoot)
    {
        var userDir = Path.Combine(webRoot, "images", "users");
        Directory.CreateDirectory(userDir);

        foreach (var user in db.Users.OrderBy(u => u.Id).ToList())
        {
            if (user.ImagePath is not null)
            {
                continue;
            }

            var file = $"user-{user.Id}.png";
            File.WriteAllBytes(
                Path.Combine(userDir, file),
                PlaceholderPng.Generate(
                    240, 240,
                    Palette(user.Id * 7 + 3),
                    Palette(user.Id * 7 + 19),
                    user.Id % 3));
            user.ImagePath = store.ToUrl($"/images/users/{file}");
        }

        db.SaveChanges();
    }

    /// <summary>Deterministic, pleasant colour for a seed slot (varied hue, medium saturation).</summary>
    private static byte[] Palette(int seed)
    {
        var hue = (seed * 67) % 360;
        var saturation = 0.45 + ((seed * 13) % 30) / 100.0;
        var value = 0.75 + ((seed * 7) % 20) / 100.0;
        return HsvToRgb(hue, saturation, value);
    }

    private static byte[] HsvToRgb(double h, double s, double v)
    {
        var c = v * s;
        var x = c * (1 - Math.Abs(h / 60 % 2 - 1));
        var m = v - c;
        var (r, g, b) = h switch
        {
            < 60 => (c, x, 0.0),
            < 120 => (x, c, 0.0),
            < 180 => (0.0, c, x),
            < 240 => (0.0, x, c),
            < 300 => (x, 0.0, c),
            _ => (c, 0.0, x),
        };
        return [(byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255)];
    }

    // --- Seed data ----------------------------------------------------------

    private static List<User> BuildUsers(IReadOnlyDictionary<string, Role> roles)
    {
        // Deterministic sign-up dates spread over the past year so the dashboard's
        // "new users (30d)" statistic reflects a realistic curve from the first boot.
        var now = DateTime.UtcNow;
        return
        [
            new User
            {
                Id = 3,
                Email = "superadmin@marketplace.dev",
                Name = "Super Admin",
                Role = roles["SuperAdmin"],
                Type = "Dashboard",
                PasswordHash = PasswordHasher.Hash("123456"),
                CreatedAt = now.AddDays(-400),
            },
            new User
            {
                Id = 1,
                Email = "admin@marketplace.dev",
                Name = "Ada Admin",
                Role = roles["Admin"],
                Type = "Dashboard",
                Phone = "+1 555 0100",
                Location = "Chicago, IL",
                Bio = "Platform administrator — catalogue and marketplace operations.",
                PasswordHash = PasswordHasher.Hash("Admin123!"),
                CreatedAt = now.AddDays(-380),
            },
            new User
            {
                Id = 4,
                Email = "manager@marketplace.dev",
                Name = "Mia Manager",
                Role = roles["Manager"],
                Type = "Dashboard",
                PasswordHash = PasswordHasher.Hash("Manager123!"),
                CreatedAt = now.AddDays(-300),
            },
            new User
            {
                Id = 5,
                Email = "chief.admin@marketplace.dev",
                Name = "Chief Admin",
                Role = roles["Admin"],
                Type = "Dashboard",
                PasswordHash = PasswordHasher.Hash("Chief123!"),
                CreatedAt = now.AddDays(-260),
            },
            new User
            {
                Id = 2,
                Email = "viewer@marketplace.dev",
                Name = "Vic Viewer",
                Role = roles["Viewer"],
                Type = "Dashboard",
                PasswordHash = PasswordHasher.Hash("Viewer123!"),
                CreatedAt = now.AddDays(-200),
            },

            // --- Mobile users ----------------------------------------------------
            new User
            {
                Id = 6,
                Email = "seller@marketplace.dev",
                Name = "Sam Seller",
                Role = roles["Provider"],
                Type = "Mobile",
                Phone = "+1 555 0142",
                Location = "Austin, TX",
                Bio = "Audio and peripherals specialist — fast shipping, 2-year warranty.",
                PasswordHash = PasswordHasher.Hash("Seller123!"),
                CreatedAt = now.AddDays(-150),
            },
            new User
            {
                Id = 7,
                Email = "nova@marketplace.dev",
                Name = "Nova Services",
                Role = roles["Provider"],
                Type = "Mobile",
                Phone = "+1 555 0177",
                Location = "Seattle, WA",
                Bio = "On-site tech support and repairs — same-day appointments.",
                PasswordHash = PasswordHasher.Hash("Nova123!"),
                CreatedAt = now.AddDays(-120),
            },
            new User
            {
                Id = 8,
                Email = "client@marketplace.dev",
                Name = "Cody Client",
                Role = roles["Client"],
                Type = "Mobile",
                Phone = "+1 555 0199",
                Location = "Denver, CO",
                PasswordHash = PasswordHasher.Hash("Client123!"),
                CreatedAt = now.AddDays(-20),
            },
        ];
    }

    private static List<Product> BuildProducts()
    {
        var createdAt = DateTime.UtcNow;
        var seed = new (string Name, string Sku, string Category, decimal Price, int Stock, int Sold)[]
        {
            ("Aurora Wireless Headset", "AUR-1001", "Audio", 149.00m, 82, 410),
            ("Nimbus Mechanical Keyboard", "NIM-2044", "Peripherals", 129.50m, 12, 268),
            ("Vertex 4K Monitor 27\"", "VTX-3310", "Displays", 429.99m, 34, 192),
            ("Pulse Ergonomic Mouse", "PLS-1180", "Peripherals", 59.90m, 140, 512),
            ("Halo Desk Lamp", "HAL-7702", "Accessories", 74.00m, 0, 88),
            ("Drift Laptop Stand", "DRF-4408", "Accessories", 44.95m, 63, 301),
            ("Quill Precision Stylus", "QUL-5521", "Accessories", 89.00m, 18, 145),
            ("Echo Smart Speaker", "ECH-6610", "Audio", 199.00m, 47, 233),
            ("Orbit Webcam Pro", "ORB-9012", "Video", 169.00m, 9, 176),
            ("Cirrus SSD 1TB", "CIR-3390", "Storage", 119.99m, 210, 640),
            ("Ember Docking Station", "EMB-2287", "Accessories", 219.00m, 26, 154),
            ("Nova USB-C Hub", "NOV-8123", "Accessories", 64.50m, 7, 389),
            ("Zenith Gaming Chair", "ZEN-4415", "Furniture", 389.00m, 21, 97),
            ("Willow Standing Desk", "WIL-7001", "Furniture", 649.00m, 14, 61),
            ("Flux Power Bank 20K", "FLX-1902", "Power", 79.90m, 128, 455),
            ("Terra Wireless Charger", "TER-3344", "Power", 39.99m, 0, 520),
            ("Lumen Ring Light", "LUM-5566", "Video", 94.00m, 55, 210),
            ("Atlas Monitor Arm", "ATL-6677", "Accessories", 139.00m, 31, 143),
            ("Kite Noise-Cancelling Buds", "KIT-7788", "Audio", 179.00m, 74, 377),
            ("Summit Team Room Kit", "SUM-9900", "Video", 1299.00m, 6, 24),
            ("Fable Notebook Pro 14\"", "FBL-2468", "Computers", 1499.00m, 19, 78),
            ("Ridge Tablet 11\"", "RDG-1357", "Computers", 899.00m, 33, 132),
            ("Coral Wireless Mic", "COR-4820", "Audio", 119.00m, 16, 205),
            ("Nimbus Smart Plug 4-Pack", "NIM-9753", "Smart Home", 49.00m, 96, 341),
        };

        var products = new List<Product>(seed.Length);
        for (var i = 0; i < seed.Length; i++)
        {
            var (name, sku, category, price, stock, sold) = seed[i];
            products.Add(new Product
            {
                Id = i + 1,
                Name = name,
                Sku = sku,
                Category = category,
                Price = price,
                Stock = stock,
                Sold = sold,
                CreatedAt = createdAt,
            });
        }

        // Demo ownership: the first third belongs to Sam Seller, the middle to Nova
        // Services, and the rest to the dashboard admin.
        for (var i = 0; i < products.Count; i++)
        {
            products[i].SellerId = i switch
            {
                < 8 => 6,
                < 16 => 7,
                _ => 1,
            };
        }

        return products;
    }

    private static List<Service> BuildServices()
    {
        var now = DateTime.UtcNow;
        return
        [
            new Service
            {
                Id = 1,
                ProviderId = 6,
                Title = "Headphone Repair & Tuning",
                Description = "Diagnostics, pad replacement and sound tuning for headphones and headsets.",
                Category = "Repair",
                Cost = 59.00m,
                ContactInfo = "+1 555 0142 · sam@marketplace.dev",
                Location = "Austin, TX",
                Offers = "20% off with any product purchase from my shop",
                CreatedAt = now,
            },
            new Service
            {
                Id = 2,
                ProviderId = 6,
                Title = "Custom Cable Build",
                Description = "Braided USB-C, audio and HDMI cables made to your length.",
                Category = "Accessories",
                Cost = 25.00m,
                ContactInfo = "+1 555 0142 · sam@marketplace.dev",
                Location = "Austin, TX",
                Offers = "Free delivery on orders over $50",
                CreatedAt = now,
            },
            new Service
            {
                Id = 3,
                ProviderId = 7,
                Title = "Home Wi-Fi Setup",
                Description = "Router placement, mesh configuration and dead-zone fixes.",
                Category = "Installation",
                Cost = 79.00m,
                ContactInfo = "+1 555 0177 · nova@marketplace.dev",
                Location = "Seattle, WA",
                Offers = "Free follow-up visit within 30 days",
                CreatedAt = now,
            },
            new Service
            {
                Id = 4,
                ProviderId = 7,
                Title = "Laptop Repair (Same Day)",
                Description = "Screen, battery and keyboard repairs for major brands.",
                Category = "Repair",
                Cost = 120.00m,
                ContactInfo = "+1 555 0177 · nova@marketplace.dev",
                Location = "Seattle, WA",
                Offers = "10% off for students",
                CreatedAt = now,
            },
            new Service
            {
                Id = 5,
                ProviderId = 7,
                Title = "Smart Home Installation",
                Description = "Setup of lights, plugs, cameras and voice assistants.",
                Category = "Installation",
                Cost = 150.00m,
                ContactInfo = "+1 555 0177 · nova@marketplace.dev",
                Location = "Seattle, WA",
                Offers = "Bundle with Wi-Fi Setup for $200",
                CreatedAt = now,
            },
            new Service
            {
                Id = 6,
                ProviderId = 1,
                Title = "Business Onboarding Consultation",
                Description = "Walkthrough of catalogue, orders and dashboard reporting.",
                Category = "Consulting",
                Cost = 200.00m,
                ContactInfo = "admin@marketplace.dev",
                Location = "Remote",
                Offers = "Free 15-minute intro call",
                CreatedAt = now,
            },
        ];
    }

    private static List<Order> BuildOrders(IReadOnlyList<Product> products, IReadOnlyList<Service> services)
    {
        var customers = new[]
        {
            "Northwind Traders", "Acme Corp", "Globex", "Initech", "Umbrella Labs",
            "Stark Supply", "Wayne Enterprises", "Hooli Retail", "Vandelay Group", "Soylent Media",
        };

        var statuses = new[] { "Completed", "Completed", "Completed", "Processing", "Refunded" };
        var random = new Random(42);
        var orders = new List<Order>(72);
        var nextOrderId = 1;

        // Six orders per month for the last 12 months so the trend chart is populated.
        for (var monthsBack = 11; monthsBack >= 0; monthsBack--)
        {
            for (var i = 0; i < 6; i++)
            {
                var product = products[random.Next(products.Count)];
                var date = DateTime.UtcNow
                    .AddMonths(-monthsBack)
                    .AddDays(-random.Next(0, 26))
                    .AddHours(-random.Next(0, 23));

                orders.Add(new Order
                {
                    Id = nextOrderId++,
                    Kind = "Product",
                    ProductId = product.Id,
                    Customer = customers[random.Next(customers.Length)],
                    Product = product.Name,
                    Category = product.Category,
                    Total = Math.Round(product.Price * random.Next(1, 4), 2),
                    Status = statuses[random.Next(statuses.Length)],
                    Date = date,
                });
            }
        }

        // Demo marketplace activity: a client purchase and a service reservation.
        orders.Add(new Order
        {
            Id = nextOrderId++,
            Kind = "Product",
            ProductId = products[0].Id,
            BuyerId = 8,
            Customer = "Cody Client",
            Product = products[0].Name,
            Category = products[0].Category,
            Total = products[0].Price,
            Status = "Completed",
            Date = DateTime.UtcNow.AddDays(-3),
        });

        orders.Add(new Order
        {
            Id = nextOrderId++,
            Kind = "Service",
            ServiceId = services[2].Id,
            BuyerId = 8,
            Customer = "Cody Client",
            Product = services[2].Title,
            Category = services[2].Category,
            Total = services[2].Cost,
            Status = "Reserved",
            Date = DateTime.UtcNow.AddDays(-1),
        });

        orders.Sort((a, b) => b.Date.CompareTo(a.Date));
        return orders;
    }
}
