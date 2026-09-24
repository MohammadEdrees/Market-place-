using MarketWorkplace.Api.Auth;
using MarketWorkplace.Api.Models;

namespace MarketWorkplace.Api.Data;

/// <summary>
/// In-memory data store seeded with sample data so the dashboard works without a database.
/// Register as a singleton: <c>builder.Services.AddSingleton&lt;InMemoryStore&gt;();</c>
/// </summary>
public class InMemoryStore
{
    private readonly object _gate = new();
    private readonly Dictionary<int, Product> _products = new();
    private readonly List<Order> _orders = [];
    private readonly List<User> _users = [];
    private int _nextProductId;
    private int _nextOrderId = 1;

    public InMemoryStore()
    {
        SeedProducts();
        SeedOrders();
        SeedUsers();
    }

    // --- Products -----------------------------------------------------------

    public IReadOnlyList<Product> GetProducts()
    {
        lock (_gate)
        {
            return _products.Values.OrderBy(p => p.Id).ToList();
        }
    }

    public Product? GetProduct(int id)
    {
        lock (_gate)
        {
            return _products.GetValueOrDefault(id);
        }
    }

    public Product CreateProduct(Product product)
    {
        lock (_gate)
        {
            product.Id = ++_nextProductId;
            product.CreatedAt = DateTime.UtcNow;
            _products[product.Id] = product;
            return product;
        }
    }

    public Product? UpdateProduct(int id, Product input)
    {
        lock (_gate)
        {
            if (!_products.TryGetValue(id, out var existing))
            {
                return null;
            }

            existing.Name = input.Name;
            existing.Sku = input.Sku;
            existing.Category = input.Category;
            existing.Price = input.Price;
            existing.Stock = input.Stock;
            return existing;
        }
    }

    public bool DeleteProduct(int id)
    {
        lock (_gate)
        {
            return _products.Remove(id);
        }
    }

    public int LowStockCount
    {
        get
        {
            lock (_gate)
            {
                return _products.Values.Count(p => p.Stock <= 15);
            }
        }
    }

    // --- Orders -------------------------------------------------------------

    public IReadOnlyList<Order> GetRecentOrders(int count = 6) =>
        _orders.OrderByDescending(o => o.Date).Take(count).ToList();

    public IReadOnlyList<Order> GetAllOrders() => _orders;

    // --- Users ---------------------------------------------------------------

    /// <summary>Finds a user by email (case-insensitive), or <c>null</c> when unknown.</summary>
    public User? FindUser(string email) =>
        _users.FirstOrDefault(u => u.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));

    // --- Seed data ----------------------------------------------------------

    private void SeedUsers()
    {
        _users.Add(new User
        {
            Id = 3,
            Email = "superadmin@marketplace.dev",
            Name = "Super Admin",
            Role = "SuperAdmin",
            PasswordHash = PasswordHasher.Hash("123456"),
        });

        _users.Add(new User
        {
            Id = 1,
            Email = "admin@marketplace.dev",
            Name = "Ada Admin",
            Role = "Admin",
            PasswordHash = PasswordHasher.Hash("Admin123!"),
        });

        _users.Add(new User
        {
            Id = 4,
            Email = "manager@marketplace.dev",
            Name = "Mia Manager",
            Role = "Manager",
            PasswordHash = PasswordHasher.Hash("Manager123!"),
        });

        _users.Add(new User
        {
            Id = 5,
            Email = "chief.admin@marketplace.dev",
            Name = "Chief Admin",
            Role = "Admin",
            PasswordHash = PasswordHasher.Hash("Chief123!"),
        });

        _users.Add(new User
        {
            Id = 2,
            Email = "viewer@marketplace.dev",
            Name = "Vic Viewer",
            Role = "Viewer",
            PasswordHash = PasswordHasher.Hash("Viewer123!"),
        });
    }

    private void SeedProducts()
    {
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

        foreach (var (name, sku, category, price, stock, sold) in seed)
        {
            CreateProduct(new Product
            {
                Name = name,
                Sku = sku,
                Category = category,
                Price = price,
                Stock = stock,
                Sold = sold,
            });
        }
    }

    private void SeedOrders()
    {
        var customers = new[]
        {
            "Northwind Traders", "Acme Corp", "Globex", "Initech", "Umbrella Labs",
            "Stark Supply", "Wayne Enterprises", "Hooli Retail", "Vandelay Group", "Soylent Media",
        };

        var statuses = new[] { "Completed", "Completed", "Completed", "Processing", "Refunded" };
        var random = new Random(42);
        var products = _products.Values.ToList();

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

                _orders.Add(new Order
                {
                    Id = _nextOrderId++,
                    Customer = customers[random.Next(customers.Length)],
                    Product = product.Name,
                    Category = product.Category,
                    Total = Math.Round(product.Price * random.Next(1, 4), 2),
                    Status = statuses[random.Next(statuses.Length)],
                    Date = date,
                });
            }
        }

        _orders.Sort((a, b) => b.Date.CompareTo(a.Date));
    }
}
