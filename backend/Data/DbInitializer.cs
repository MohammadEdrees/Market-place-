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

        if (!db.Orders.Any())
        {
            db.Orders.AddRange(BuildOrders(products));
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
            PasswordHash = PasswordHasher.Hash("123456"),
        },
        new User
        {
            Id = 1,
            Email = "admin@marketplace.dev",
            Name = "Ada Admin",
            Role = "Admin",
            PasswordHash = PasswordHasher.Hash("Admin123!"),
        },
        new User
        {
            Id = 4,
            Email = "manager@marketplace.dev",
            Name = "Mia Manager",
            Role = "Manager",
            PasswordHash = PasswordHasher.Hash("Manager123!"),
        },
        new User
        {
            Id = 5,
            Email = "chief.admin@marketplace.dev",
            Name = "Chief Admin",
            Role = "Admin",
            PasswordHash = PasswordHasher.Hash("Chief123!"),
        },
        new User
        {
            Id = 2,
            Email = "viewer@marketplace.dev",
            Name = "Vic Viewer",
            Role = "Viewer",
            PasswordHash = PasswordHasher.Hash("Viewer123!"),
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

        return products;
    }

    private static List<Order> BuildOrders(IReadOnlyList<Product> products)
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
                    Customer = customers[random.Next(customers.Length)],
                    Product = product.Name,
                    Category = product.Category,
                    Total = Math.Round(product.Price * random.Next(1, 4), 2),
                    Status = statuses[random.Next(statuses.Length)],
                    Date = date,
                });
            }
        }

        orders.Sort((a, b) => b.Date.CompareTo(a.Date));
        return orders;
    }
}
