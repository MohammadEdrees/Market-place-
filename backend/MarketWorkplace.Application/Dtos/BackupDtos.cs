namespace MarketWorkplace.Application.Dtos;

/// <summary>
/// A full JSON snapshot of the marketplace dataset, as written by <c>GET /api/backup/export</c>
/// and consumed by <c>POST /api/backup/restore</c>.
/// </summary>
/// <param name="Version">Format version; restores refuse anything they do not understand.</param>
/// <param name="CreatedAtUtc">When the snapshot was taken.</param>
/// <param name="App">Marker identifying the producing application.</param>
/// <param name="Counts">Row count per table, for a quick look without loading the file.</param>
/// <param name="Roles">Role definitions.</param>
/// <param name="Users">Accounts, including their password hashes so a restore keeps sign-ins working.</param>
/// <param name="Categories">Managed product/service category names.</param>
/// <param name="Products">Catalogue products.</param>
/// <param name="Services">Offered services.</param>
/// <param name="Images">Gallery image rows (the files themselves live in wwwroot and are not included).</param>
/// <param name="Orders">Order history.</param>
/// <param name="Advertisements">Advertisement slides.</param>
/// <param name="Subscriptions">Subscription plans; omitted by snapshots written before that
/// table existed, which restore as "no subscriptions" instead of being rejected.</param>
public record BackupSnapshot(
    int Version,
    DateTime CreatedAtUtc,
    string App,
    IReadOnlyDictionary<string, int> Counts,
    IReadOnlyList<RoleSnapshot> Roles,
    IReadOnlyList<UserSnapshot> Users,
    IReadOnlyList<CategorySnapshot> Categories,
    IReadOnlyList<ProductSnapshot> Products,
    IReadOnlyList<ServiceSnapshot> Services,
    IReadOnlyList<ImageSnapshot> Images,
    IReadOnlyList<OrderSnapshot> Orders,
    IReadOnlyList<AdvertisementSnapshot> Advertisements,
    IReadOnlyList<SubscriptionSnapshot>? Subscriptions = null);

/// <summary>One role inside a backup file.</summary>
public record RoleSnapshot(int Id, string Name, string? Description);

/// <summary>One account inside a backup file (password hash included so restores keep sign-ins working).</summary>
public record UserSnapshot(
    int Id,
    string Email,
    string Name,
    string PasswordHash,
    int RoleId,
    string Type,
    string? Phone,
    string? Location,
    string? Bio,
    string? ImagePath,
    DateTime CreatedAt);

/// <summary>One managed category inside a backup file.</summary>
public record CategorySnapshot(int Id, string Name, string Kind, DateTime CreatedAt);

/// <summary>One product inside a backup file.</summary>
public record ProductSnapshot(
    int Id,
    string Name,
    string Sku,
    string Category,
    decimal Price,
    int Stock,
    int Sold,
    int? SellerId,
    DateTime CreatedAt);

/// <summary>One service inside a backup file.</summary>
public record ServiceSnapshot(
    int Id,
    string Title,
    string Description,
    string Category,
    decimal Cost,
    string ContactInfo,
    string Location,
    string? Offers,
    int ProviderId,
    bool IsActive,
    DateTime CreatedAt);

/// <summary>One gallery image row inside a backup file.</summary>
public record ImageSnapshot(
    int Id,
    int? ProductId,
    int? ServiceId,
    string Path,
    int SortOrder,
    DateTime CreatedAt);

/// <summary>One order inside a backup file.</summary>
public record OrderSnapshot(
    int Id,
    string Customer,
    string Product,
    string Category,
    decimal Total,
    string Status,
    DateTime Date,
    string Kind,
    int? ProductId,
    int? ServiceId,
    int? BuyerId);

/// <summary>One advertisement inside a backup file.</summary>
public record AdvertisementSnapshot(
    int Id,
    string Title,
    string? Subtitle,
    string? ImagePath,
    string? TargetUrl,
    bool IsActive,
    DateTime? StartsAt,
    DateTime? EndsAt,
    int SortOrder,
    DateTime CreatedAt);

/// <summary>One subscription plan inside a backup file.</summary>
public record SubscriptionSnapshot(
    int Id,
    int UserId,
    string Plan,
    decimal Price,
    string BillingCycle,
    string Status,
    DateTime StartsAt,
    DateTime? EndsAt,
    bool AutoRenew,
    DateTime CreatedAt);

/// <summary>A stored snapshot listed by <c>GET /api/backup</c>.</summary>
/// <param name="Name">File name inside the server-side backup folder.</param>
/// <param name="CreatedAtUtc">When the snapshot was written.</param>
/// <param name="SizeBytes">File size on disk.</param>
/// <param name="Counts">Row counts recorded inside the file.</param>
public record BackupFileDto(
    string Name,
    DateTime CreatedAtUtc,
    long SizeBytes,
    IReadOnlyDictionary<string, int> Counts);

/// <summary>Per-table outcome of a restore.</summary>
/// <param name="Table">Table name as it appears in the backup file.</param>
/// <param name="Inserted">Rows added.</param>
/// <param name="Updated">Existing rows overwritten (merge mode only).</param>
public record RestoreTableResultDto(string Table, int Inserted, int Updated);

/// <summary>Result of <c>POST /api/backup/restore</c>.</summary>
/// <param name="Mode"><c>merge</c> or <c>replace</c>.</param>
/// <param name="RestoredAtUtc">When the restore was applied.</param>
/// <param name="Tables">Per-table inserted/updated counts.</param>
/// <param name="TotalInserted">Sum of all inserted rows.</param>
/// <param name="TotalUpdated">Sum of all updated rows.</param>
public record RestoreReportDto(
    string Mode,
    DateTime RestoredAtUtc,
    IReadOnlyList<RestoreTableResultDto> Tables,
    int TotalInserted,
    int TotalUpdated);
