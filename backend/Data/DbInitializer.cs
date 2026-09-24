using MarketWorkplace.Api.Auth;
using MarketWorkplace.Api.Models;

namespace MarketWorkplace.Api.Data;

/// <summary>
/// Creates the schema and loads the sample data (products, orders and users) that used to
/// live in <c>InMemoryStore</c>. Runs once at startup; each collection is seeded only when
/// it is empty, so restarts keep whatever is already in the store.
/// </summary>
public static class DbInitializer
{
    /// <summary>Ensures the model exists and seeds any missing collections.</summary>
    public static void Initialize(MarketDbContext db)
    {
        db.Database.EnsureCreated();

        if (!db.Users.Any())
        {
            db.Users.AddRange(BuildUsers());
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
    }

    // --- Seed data ----------------------------------------------------------

    private static List<User> BuildUsers() =>
    [
        new User
        {
            Id = 3,
            Email = "superadmin@marketplace.dev",
            Name = "Super Admin",
            Role = "SuperAdmin",
            Type = "Dashboard",
            PasswordHash = PasswordHasher.Hash("123456"),
        },
        new User
        {
            Id = 1,
            Email = "admin@marketplace.dev",
            Name = "Ada Admin",
            Role = "Admin",
            Type = "Dashboard",
            Phone = "+1 555 0100",
            Location = "Chicago, IL",
            Bio = "Platform administrator — catalogue and marketplace operations.",
            PasswordHash = PasswordHasher.Hash("Admin123!"),
        },
        new User
        {
            Id = 4,
            Email = "manager@marketplace.dev",
            Name = "Mia Manager",
            Role = "Manager",
            Type = "Dashboard",
            PasswordHash = PasswordHasher.Hash("Manager123!"),
        },
        new User
        {
            Id = 5,
            Email = "chief.admin@marketplace.dev",
            Name = "Chief Admin",
            Role = "Admin",
            Type = "Dashboard",
            PasswordHash = PasswordHasher.Hash("Chief123!"),
        },
        new User
        {
            Id = 2,
            Email = "viewer@marketplace.dev",
            Name = "Vic Viewer",
            Role = "Viewer",
            Type = "Dashboard",
            PasswordHash = PasswordHasher.Hash("Viewer123!"),
        },

        // --- Mobile users ----------------------------------------------------
        new User
        {
            Id = 6,
            Email = "seller@marketplace.dev",
            Name = "Sam Seller",
            Role = "Provider",
            Type = "Mobile",
            Phone = "+1 555 0142",
            Location = "Austin, TX",
            Bio = "Audio and peripherals specialist — fast shipping, 2-year warranty.",
            PasswordHash = PasswordHasher.Hash("Seller123!"),
        },
        new User
        {
            Id = 7,
            Email = "nova@marketplace.dev",
            Name = "Nova Services",
            Role = "Provider",
            Type = "Mobile",
            Phone = "+1 555 0177",
            Location = "Seattle, WA",
            Bio = "On-site tech support and repairs — same-day appointments.",
            PasswordHash = PasswordHasher.Hash("Nova123!"),
        },
        new User
        {
            Id = 8,
            Email = "client@marketplace.dev",
            Name = "Cody Client",
            Role = "Client",
            Type = "Mobile",
            Phone = "+1 555 0199",
            Location = "Denver, CO",
            PasswordHash = PasswordHasher.Hash("Client123!"),
        },
    ];

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
