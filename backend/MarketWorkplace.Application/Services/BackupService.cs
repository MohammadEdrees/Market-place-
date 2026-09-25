using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using MarketWorkplace.Application.Common;
using MarketWorkplace.Application.Dtos;
using MarketWorkplace.Application.Interfaces;
using MarketWorkplace.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static MarketWorkplace.Application.Common.ServiceResults;

namespace MarketWorkplace.Application.Services;

/// <summary>
/// Backup and restore for the whole marketplace dataset: <c>GET /api/backup/export</c> writes a
/// JSON snapshot of every table (and keeps a server-side copy under <c>App_Data/backups</c>),
/// while <c>POST /api/backup/restore</c> applies one back — either merging it into the live data
/// or replacing everything. Every operation is admin-only: snapshots contain password hashes.
/// </summary>
/// <remarks>
/// Uploaded image <i>files</i> (wwwroot/images) are not part of a snapshot — only the image rows
/// that reference them. Restoring on another machine therefore needs its media copied separately.
/// </remarks>
public class BackupService(
    IRepository<Role> roles,
    IRepository<User> users,
    IRepository<Category> categories,
    IRepository<Product> products,
    IRepository<Service> services,
    IRepository<ListingImage> images,
    IRepository<Order> orders,
    IRepository<Advertisement> advertisements,
    IBackupStore store)
{
    /// <summary>Format version written into every snapshot; restores refuse anything else.</summary>
    public const int CurrentVersion = 1;

    /// <summary>Upper bound for an uploaded snapshot (mirrored by the controller's request limit).</summary>
    public const int MaxBackupBytes = 25 * 1024 * 1024;

    /// <summary>How many stored snapshots to keep; older files are pruned on every export.</summary>
    private const int RetainedBackups = 10;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    // ---------------------------------------------------------------- export

    /// <summary>Every stored snapshot with its row counts (admin callers only).</summary>
    public ActionResult<IEnumerable<BackupFileDto>> List(ClaimsPrincipal user)
    {
        if (!Access.IsAdmin(user))
        {
            return Forbid();
        }

        return Ok(store.List()
            .Select(file => new BackupFileDto(file.Name, file.CreatedAtUtc, file.SizeBytes, ReadCounts(file.Name)))
            .ToList());
    }

    /// <summary>
    /// Snapshots the live data, stores a copy server-side and returns the file for download
    /// (admin callers only).
    /// </summary>
    public ActionResult<(string Name, byte[] Content)> Export(ClaimsPrincipal user)
    {
        if (!Access.IsAdmin(user))
        {
            return Forbid();
        }

        var content = JsonSerializer.SerializeToUtf8Bytes(BuildSnapshot(), JsonOptions);

        // Milliseconds in the name: two exports in the same second must not overwrite each other.
        var name = $"marketplace-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.json";

        store.Save(name, content);
        store.Prune(RetainedBackups);

        return Ok((name, content));
    }

    /// <summary>Downloads a stored snapshot (admin callers only).</summary>
    public ActionResult<byte[]> Download(ClaimsPrincipal user, string name)
    {
        if (!Access.IsAdmin(user))
        {
            return Forbid();
        }

        var content = store.Read(name);
        return content is null ? NotFound() : Ok(content);
    }

    /// <summary>Deletes a stored snapshot (admin callers only).</summary>
    public ActionResult DeleteBackup(ClaimsPrincipal user, string name)
    {
        if (!Access.IsAdmin(user))
        {
            return Forbid();
        }

        return store.Delete(name) ? NoContent() : NotFound();
    }

    // --------------------------------------------------------------- restore

    /// <summary>
    /// Applies a backup file: <c>merge</c> upserts rows by id and never deletes, <c>replace</c>
    /// wipes every table first (admin callers only, and transactional either way).
    /// </summary>
    /// <param name="content">Raw JSON bytes of the snapshot.</param>
    /// <param name="mode"><c>merge</c> (default) or <c>replace</c>.</param>
    public ActionResult<RestoreReportDto> Restore(ClaimsPrincipal user, byte[]? content, string? mode)
    {
        if (!Access.IsAdmin(user))
        {
            return Forbid();
        }

        if (content is null || content.Length == 0)
        {
            return Problem("Empty backup file.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (content.Length > MaxBackupBytes)
        {
            return Problem(
                "Backup file too large.",
                $"Backups are limited to {MaxBackupBytes / 1024 / 1024} MB.",
                StatusCodes.Status400BadRequest);
        }

        BackupSnapshot? snapshot;
        try
        {
            snapshot = JsonSerializer.Deserialize<BackupSnapshot>(content, JsonOptions);
        }
        catch (JsonException ex)
        {
            return Problem("Not a valid backup file.", ex.Message, StatusCodes.Status400BadRequest);
        }

        if (snapshot is null)
        {
            return Problem(
                "Not a valid backup file.",
                "The file could not be read as a Market Workplace backup.",
                StatusCodes.Status400BadRequest);
        }

        if (snapshot.Version != CurrentVersion)
        {
            return Problem(
                "Unsupported backup version.",
                $"This API reads version {CurrentVersion}; the file is version {snapshot.Version}.",
                StatusCodes.Status400BadRequest);
        }

        if (snapshot.Roles is null || snapshot.Users is null || snapshot.Categories is null ||
            snapshot.Products is null || snapshot.Services is null || snapshot.Images is null ||
            snapshot.Orders is null || snapshot.Advertisements is null)
        {
            return Problem(
                "Incomplete backup file.",
                "One or more tables are missing.",
                StatusCodes.Status400BadRequest);
        }

        var replace = string.Equals(mode, "replace", StringComparison.OrdinalIgnoreCase);
        var report = new List<RestoreTableResultDto>();
        var restoredAt = DateTime.UtcNow;

        try
        {
            if (replace)
            {
                // Deletes and inserts of the same ids cannot share one SaveChanges, so the two
                // phases are sequenced — the transaction keeps the whole restore all-or-nothing.
                products.InTransaction(() =>
                {
                    WipeEverything();
                    report.AddRange(InsertAll(snapshot));
                });
            }
            else
            {
                report.AddRange(MergeAll(snapshot));
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            return Problem("The backup could not be applied.", ex.Message, StatusCodes.Status400BadRequest);
        }

        return Ok(new RestoreReportDto(
            replace ? "replace" : "merge",
            restoredAt,
            report,
            report.Sum(r => r.Inserted),
            report.Sum(r => r.Updated)));
    }

    // ---------------------------------------------------------------- shared

    /// <summary>Reads every table into a snapshot with its row counts.</summary>
    private BackupSnapshot BuildSnapshot()
    {
        var roleRows = roles.QueryReadOnly().OrderBy(r => r.Id).ToList();
        var userRows = users.QueryReadOnly().OrderBy(u => u.Id).ToList();
        var categoryRows = categories.QueryReadOnly().OrderBy(c => c.Id).ToList();
        var productRows = products.QueryReadOnly().OrderBy(p => p.Id).ToList();
        var serviceRows = services.QueryReadOnly().OrderBy(s => s.Id).ToList();
        var imageRows = images.QueryReadOnly().OrderBy(i => i.Id).ToList();
        var orderRows = orders.QueryReadOnly().OrderBy(o => o.Id).ToList();
        var adRows = advertisements.QueryReadOnly().OrderBy(a => a.Id).ToList();

        var counts = new Dictionary<string, int>
        {
            ["roles"] = roleRows.Count,
            ["users"] = userRows.Count,
            ["categories"] = categoryRows.Count,
            ["products"] = productRows.Count,
            ["services"] = serviceRows.Count,
            ["images"] = imageRows.Count,
            ["orders"] = orderRows.Count,
            ["advertisements"] = adRows.Count,
        };

        return new BackupSnapshot(
            CurrentVersion,
            DateTime.UtcNow,
            "MarketWorkplace",
            counts,
            roleRows.Select(r => new RoleSnapshot(r.Id, r.Name, r.Description)).ToList(),
            userRows.Select(u => new UserSnapshot(
                u.Id, u.Email, u.Name, u.PasswordHash, u.RoleId, u.Type,
                u.Phone, u.Location, u.Bio, u.ImagePath, u.CreatedAt)).ToList(),
            categoryRows.Select(c => new CategorySnapshot(c.Id, c.Name, c.Kind, c.CreatedAt)).ToList(),
            productRows.Select(p => new ProductSnapshot(
                p.Id, p.Name, p.Sku, p.Category, p.Price, p.Stock, p.Sold, p.SellerId, p.CreatedAt)).ToList(),
            serviceRows.Select(s => new ServiceSnapshot(
                s.Id, s.Title, s.Description, s.Category, s.Cost, s.ContactInfo,
                s.Location, s.Offers, s.ProviderId, s.IsActive, s.CreatedAt)).ToList(),
            imageRows.Select(i => new ImageSnapshot(
                i.Id, i.ProductId, i.ServiceId, i.Path, i.SortOrder, i.CreatedAt)).ToList(),
            orderRows.Select(o => new OrderSnapshot(
                o.Id, o.Customer, o.Product, o.Category, o.Total, o.Status,
                o.Date, o.Kind, o.ProductId, o.ServiceId, o.BuyerId)).ToList(),
            adRows.Select(a => new AdvertisementSnapshot(
                a.Id, a.Title, a.Subtitle, a.ImagePath, a.TargetUrl, a.IsActive,
                a.StartsAt, a.EndsAt, a.SortOrder, a.CreatedAt)).ToList());
    }

    /// <summary>Row counts recorded inside a stored snapshot, or an empty map when unreadable.</summary>
    private IReadOnlyDictionary<string, int> ReadCounts(string name)
    {
        try
        {
            var bytes = store.Read(name);
            if (bytes is null)
            {
                return new Dictionary<string, int>();
            }

            using var document = JsonDocument.Parse(bytes);
            if (!document.RootElement.TryGetProperty("counts", out var counts) || counts.ValueKind != JsonValueKind.Object)
            {
                return new Dictionary<string, int>();
            }

            return counts.EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value.TryGetInt32(out var value) ? value : 0);
        }
        catch (JsonException)
        {
            return new Dictionary<string, int>();
        }
    }

    /// <summary>Empties every table in foreign-key-safe order (replace mode).</summary>
    private void WipeEverything()
    {
        images.RemoveRange(images.Query());
        orders.RemoveRange(orders.Query());
        services.RemoveRange(services.Query());
        products.RemoveRange(products.Query());
        advertisements.RemoveRange(advertisements.Query());
        users.RemoveRange(users.Query());
        categories.RemoveRange(categories.Query());
        roles.RemoveRange(roles.Query());
        SaveAll();
    }

    private void SaveAll()
    {
        roles.SaveChanges();
        users.SaveChanges();
        categories.SaveChanges();
        products.SaveChanges();
        services.SaveChanges();
        images.SaveChanges();
        orders.SaveChanges();
        advertisements.SaveChanges();
    }

    /// <summary>Inserts every row of a snapshot (replace mode).</summary>
    private IEnumerable<RestoreTableResultDto> InsertAll(BackupSnapshot snapshot)
    {
        var results = new List<RestoreTableResultDto>();

        roles.AddRange(snapshot.Roles.Select(s => new Role { Id = s.Id, Name = s.Name, Description = s.Description }));
        users.AddRange(snapshot.Users.Select(s => new User
        {
            Id = s.Id, Email = s.Email, Name = s.Name, PasswordHash = s.PasswordHash, RoleId = s.RoleId,
            Type = s.Type, Phone = s.Phone, Location = s.Location, Bio = s.Bio, ImagePath = s.ImagePath,
            CreatedAt = s.CreatedAt,
        }));
        categories.AddRange(snapshot.Categories.Select(s => new Category { Id = s.Id, Name = s.Name, Kind = s.Kind, CreatedAt = s.CreatedAt }));
        products.AddRange(snapshot.Products.Select(s => new Product
        {
            Id = s.Id, Name = s.Name, Sku = s.Sku, Category = s.Category, Price = s.Price,
            Stock = s.Stock, Sold = s.Sold, SellerId = s.SellerId, CreatedAt = s.CreatedAt,
        }));
        services.AddRange(snapshot.Services.Select(s => new Service
        {
            Id = s.Id, Title = s.Title, Description = s.Description, Category = s.Category, Cost = s.Cost,
            ContactInfo = s.ContactInfo, Location = s.Location, Offers = s.Offers ?? string.Empty,
            ProviderId = s.ProviderId, IsActive = s.IsActive, CreatedAt = s.CreatedAt,
        }));
        images.AddRange(snapshot.Images.Select(s => new ListingImage
        {
            Id = s.Id, ProductId = s.ProductId, ServiceId = s.ServiceId, Path = s.Path,
            SortOrder = s.SortOrder, CreatedAt = s.CreatedAt,
        }));
        orders.AddRange(snapshot.Orders.Select(s => new Order
        {
            Id = s.Id, Customer = s.Customer, Product = s.Product, Category = s.Category, Total = s.Total,
            Status = s.Status, Date = s.Date, Kind = s.Kind, ProductId = s.ProductId,
            ServiceId = s.ServiceId, BuyerId = s.BuyerId,
        }));
        advertisements.AddRange(snapshot.Advertisements.Select(s => new Advertisement
        {
            Id = s.Id, Title = s.Title, Subtitle = s.Subtitle, ImagePath = s.ImagePath, TargetUrl = s.TargetUrl,
            IsActive = s.IsActive, StartsAt = s.StartsAt, EndsAt = s.EndsAt, SortOrder = s.SortOrder,
            CreatedAt = s.CreatedAt,
        }));

        SaveAll();

        results.Add(new RestoreTableResultDto("roles", snapshot.Roles.Count, 0));
        results.Add(new RestoreTableResultDto("users", snapshot.Users.Count, 0));
        results.Add(new RestoreTableResultDto("categories", snapshot.Categories.Count, 0));
        results.Add(new RestoreTableResultDto("products", snapshot.Products.Count, 0));
        results.Add(new RestoreTableResultDto("services", snapshot.Services.Count, 0));
        results.Add(new RestoreTableResultDto("images", snapshot.Images.Count, 0));
        results.Add(new RestoreTableResultDto("orders", snapshot.Orders.Count, 0));
        results.Add(new RestoreTableResultDto("advertisements", snapshot.Advertisements.Count, 0));
        return results;
    }

    /// <summary>
    /// Upserts every row of a snapshot by id without deleting anything. Optional foreign keys whose
    /// target is missing are cleared; a required one (service provider) aborts the restore.
    /// </summary>
    private IEnumerable<RestoreTableResultDto> MergeAll(BackupSnapshot snapshot)
    {
        var results = new List<RestoreTableResultDto>();

        // --- roles
        var roleRows = roles.Query().ToList();
        var roleById = roleRows.ToDictionary(r => r.Id);
        var roleInserted = 0;
        var roleUpdated = 0;
        foreach (var s in snapshot.Roles)
        {
            if (roleById.TryGetValue(s.Id, out var existing))
            {
                existing.Name = s.Name;
                existing.Description = s.Description;
                roleUpdated++;
            }
            else
            {
                var added = new Role { Id = s.Id, Name = s.Name, Description = s.Description };
                roles.Add(added);
                roleById[s.Id] = added;
                roleInserted++;
            }
        }

        var roleIds = roleById.Keys.ToHashSet();
        results.Add(new RestoreTableResultDto("roles", roleInserted, roleUpdated));

        // --- users (role is required)
        var userRows = users.Query().ToList();
        var userById = userRows.ToDictionary(u => u.Id);
        var userInserted = 0;
        var userUpdated = 0;
        foreach (var s in snapshot.Users)
        {
            if (!roleIds.Contains(s.RoleId))
            {
                throw new InvalidOperationException(
                    $"Account '{s.Email}' references role {s.RoleId}, which the backup does not contain.");
            }

            if (userById.TryGetValue(s.Id, out var existing))
            {
                existing.Email = s.Email;
                existing.Name = s.Name;
                existing.PasswordHash = s.PasswordHash;
                existing.RoleId = s.RoleId;
                existing.Type = s.Type;
                existing.Phone = s.Phone;
                existing.Location = s.Location;
                existing.Bio = s.Bio;
                existing.ImagePath = s.ImagePath;
                existing.CreatedAt = s.CreatedAt;
                userUpdated++;
            }
            else
            {
                var added = new User
                {
                    Id = s.Id, Email = s.Email, Name = s.Name, PasswordHash = s.PasswordHash, RoleId = s.RoleId,
                    Type = s.Type, Phone = s.Phone, Location = s.Location, Bio = s.Bio, ImagePath = s.ImagePath,
                    CreatedAt = s.CreatedAt,
                };
                users.Add(added);
                userById[s.Id] = added;
                userInserted++;
            }
        }

        var userIds = userById.Keys.ToHashSet();
        results.Add(new RestoreTableResultDto("users", userInserted, userUpdated));

        // --- categories
        var categoryRows = categories.Query().ToList();
        var categoryById = categoryRows.ToDictionary(c => c.Id);
        var categoryInserted = 0;
        var categoryUpdated = 0;
        foreach (var s in snapshot.Categories)
        {
            if (categoryById.TryGetValue(s.Id, out var existing))
            {
                existing.Name = s.Name;
                existing.Kind = s.Kind;
                categoryUpdated++;
            }
            else
            {
                var added = new Category { Id = s.Id, Name = s.Name, Kind = s.Kind, CreatedAt = s.CreatedAt };
                categories.Add(added);
                categoryById[s.Id] = added;
                categoryInserted++;
            }
        }

        results.Add(new RestoreTableResultDto("categories", categoryInserted, categoryUpdated));

        // --- products
        var productRows = products.Query().ToList();
        var productById = productRows.ToDictionary(p => p.Id);
        var productInserted = 0;
        var productUpdated = 0;
        foreach (var s in snapshot.Products)
        {
            var sellerId = s.SellerId is int seller && userIds.Contains(seller) ? seller : (int?)null;
            if (productById.TryGetValue(s.Id, out var existing))
            {
                existing.Name = s.Name;
                existing.Sku = s.Sku;
                existing.Category = s.Category;
                existing.Price = s.Price;
                existing.Stock = s.Stock;
                existing.Sold = s.Sold;
                existing.SellerId = sellerId;
                productUpdated++;
            }
            else
            {
                var added = new Product
                {
                    Id = s.Id, Name = s.Name, Sku = s.Sku, Category = s.Category, Price = s.Price,
                    Stock = s.Stock, Sold = s.Sold, SellerId = sellerId, CreatedAt = s.CreatedAt,
                };
                products.Add(added);
                productById[s.Id] = added;
                productInserted++;
            }
        }

        var productIds = productById.Keys.ToHashSet();
        results.Add(new RestoreTableResultDto("products", productInserted, productUpdated));

        // --- services (provider is required)
        var serviceRows = services.Query().ToList();
        var serviceById = serviceRows.ToDictionary(s => s.Id);
        var serviceInserted = 0;
        var serviceUpdated = 0;
        foreach (var s in snapshot.Services)
        {
            if (!userIds.Contains(s.ProviderId))
            {
                throw new InvalidOperationException(
                    $"Service '{s.Title}' references provider {s.ProviderId}, which the backup does not contain.");
            }

            if (serviceById.TryGetValue(s.Id, out var existing))
            {
                existing.Title = s.Title;
                existing.Description = s.Description;
                existing.Category = s.Category;
                existing.Cost = s.Cost;
                existing.ContactInfo = s.ContactInfo;
                existing.Location = s.Location;
                existing.Offers = s.Offers ?? string.Empty;
                existing.ProviderId = s.ProviderId;
                existing.IsActive = s.IsActive;
                serviceUpdated++;
            }
            else
            {
                var added = new Service
                {
                    Id = s.Id, Title = s.Title, Description = s.Description, Category = s.Category,
                    Cost = s.Cost, ContactInfo = s.ContactInfo, Location = s.Location, Offers = s.Offers ?? string.Empty,
                    ProviderId = s.ProviderId, IsActive = s.IsActive, CreatedAt = s.CreatedAt,
                };
                services.Add(added);
                serviceById[s.Id] = added;
                serviceInserted++;
            }
        }

        var serviceIds = serviceById.Keys.ToHashSet();
        results.Add(new RestoreTableResultDto("services", serviceInserted, serviceUpdated));

        // --- gallery images
        var imageRows = images.Query().ToList();
        var imageById = imageRows.ToDictionary(i => i.Id);
        var imageInserted = 0;
        var imageUpdated = 0;
        foreach (var s in snapshot.Images)
        {
            var productId = s.ProductId is int p && productIds.Contains(p) ? p : (int?)null;
            var serviceId = s.ServiceId is int sv && serviceIds.Contains(sv) ? sv : (int?)null;
            if (imageById.TryGetValue(s.Id, out var existing))
            {
                existing.ProductId = productId;
                existing.ServiceId = serviceId;
                existing.Path = s.Path;
                existing.SortOrder = s.SortOrder;
                imageUpdated++;
            }
            else
            {
                var added = new ListingImage
                {
                    Id = s.Id, ProductId = productId, ServiceId = serviceId, Path = s.Path,
                    SortOrder = s.SortOrder, CreatedAt = s.CreatedAt,
                };
                images.Add(added);
                imageById[s.Id] = added;
                imageInserted++;
            }
        }

        results.Add(new RestoreTableResultDto("images", imageInserted, imageUpdated));

        // --- orders
        var orderRows = orders.Query().ToList();
        var orderById = orderRows.ToDictionary(o => o.Id);
        var orderInserted = 0;
        var orderUpdated = 0;
        foreach (var s in snapshot.Orders)
        {
            var buyerId = s.BuyerId is int b && userIds.Contains(b) ? b : (int?)null;
            var productId = s.ProductId is int p && productIds.Contains(p) ? p : (int?)null;
            var serviceId = s.ServiceId is int sv && serviceIds.Contains(sv) ? sv : (int?)null;
            if (orderById.TryGetValue(s.Id, out var existing))
            {
                existing.Customer = s.Customer;
                existing.Product = s.Product;
                existing.Category = s.Category;
                existing.Total = s.Total;
                existing.Status = s.Status;
                existing.Date = s.Date;
                existing.Kind = s.Kind;
                existing.ProductId = productId;
                existing.ServiceId = serviceId;
                existing.BuyerId = buyerId;
                orderUpdated++;
            }
            else
            {
                var added = new Order
                {
                    Id = s.Id, Customer = s.Customer, Product = s.Product, Category = s.Category,
                    Total = s.Total, Status = s.Status, Date = s.Date, Kind = s.Kind,
                    ProductId = productId, ServiceId = serviceId, BuyerId = buyerId,
                };
                orders.Add(added);
                orderById[s.Id] = added;
                orderInserted++;
            }
        }

        results.Add(new RestoreTableResultDto("orders", orderInserted, orderUpdated));

        // --- advertisements
        var adRows = advertisements.Query().ToList();
        var adById = adRows.ToDictionary(a => a.Id);
        var adInserted = 0;
        var adUpdated = 0;
        foreach (var s in snapshot.Advertisements)
        {
            if (adById.TryGetValue(s.Id, out var existing))
            {
                existing.Title = s.Title;
                existing.Subtitle = s.Subtitle;
                existing.ImagePath = s.ImagePath;
                existing.TargetUrl = s.TargetUrl;
                existing.IsActive = s.IsActive;
                existing.StartsAt = s.StartsAt;
                existing.EndsAt = s.EndsAt;
                existing.SortOrder = s.SortOrder;
                adUpdated++;
            }
            else
            {
                var added = new Advertisement
                {
                    Id = s.Id, Title = s.Title, Subtitle = s.Subtitle, ImagePath = s.ImagePath,
                    TargetUrl = s.TargetUrl, IsActive = s.IsActive, StartsAt = s.StartsAt,
                    EndsAt = s.EndsAt, SortOrder = s.SortOrder, CreatedAt = s.CreatedAt,
                };
                advertisements.Add(added);
                adById[s.Id] = added;
                adInserted++;
            }
        }

        results.Add(new RestoreTableResultDto("advertisements", adInserted, adUpdated));

        SaveAll();
        return results;
    }
}
