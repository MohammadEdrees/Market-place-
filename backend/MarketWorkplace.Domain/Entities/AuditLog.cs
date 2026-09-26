using System.ComponentModel.DataAnnotations;

namespace MarketWorkplace.Domain.Entities;

/// <summary>
/// One recorded API mutation. Rows are appended by <c>AuditLogMiddleware</c>
/// after the response completes, so the trail covers every 2xx
/// <c>POST</c>/<c>PUT</c>/<c>DELETE</c> the API serves — including sign-ins.
/// </summary>
/// <remarks>
/// The actor is stored as a snapshot (<see cref="UserName"/>, <see cref="Role"/>)
/// rather than a live join, so the entry still reads correctly after the account
/// is renamed or deleted.
/// </remarks>
public class AuditLog
{
    /// <summary>Database-generated key — the one table whose rows are appended
    /// concurrently outside any request's unit of work.</summary>
    public int Id { get; set; }

    /// <summary>When the call finished, in UTC.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>The caller's id from the JWT <c>sub</c> claim, or <c>null</c> when anonymous.</summary>
    public int? UserId { get; set; }

    /// <summary>Display name at the time of the call (the sign-in e-mail for anonymous auth calls).</summary>
    [StringLength(254)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>Role at the time of the call; empty for anonymous callers.</summary>
    [StringLength(40)]
    public string Role { get; set; } = string.Empty;

    /// <summary>What happened: <c>create</c>, <c>update</c>, <c>delete</c>, <c>login</c>, <c>register</c> or <c>restore</c>.</summary>
    [StringLength(12)]
    public string Action { get; set; } = string.Empty;

    /// <summary>The route family, e.g. <c>products</c>, <c>users</c>, <c>backup</c>.</summary>
    [StringLength(40)]
    public string Entity { get; set; } = string.Empty;

    /// <summary>The first numeric route segment after the entity, when there is one.</summary>
    public int? EntityId { get; set; }

    /// <summary>The raw request path, e.g. <c>/api/products/12/images/7</c>.</summary>
    [StringLength(260)]
    public string Path { get; set; } = string.Empty;

    /// <summary>The HTTP status returned (always 2xx — failures are not recorded).</summary>
    public int StatusCode { get; set; }

    /// <summary>Server-side duration of the whole request, in milliseconds.</summary>
    public int DurationMs { get; set; }
}
