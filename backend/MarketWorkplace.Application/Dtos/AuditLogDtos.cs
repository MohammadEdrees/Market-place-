namespace MarketWorkplace.Application.Dtos;

/// <summary>One entry of the audit trail as returned by <c>GET /api/auditlogs</c>.</summary>
/// <param name="Id">Entry identifier.</param>
/// <param name="CreatedAt">When the call finished (UTC).</param>
/// <param name="UserId">The caller's id, or <c>null</c> when anonymous.</param>
/// <param name="UserName">The caller's name at the time of the call.</param>
/// <param name="Role">The caller's role at the time of the call.</param>
/// <param name="Action"><c>create</c>, <c>update</c>, <c>delete</c>, <c>login</c>, <c>register</c> or <c>restore</c>.</param>
/// <param name="Entity">The route family, e.g. <c>products</c>.</param>
/// <param name="EntityId">The first numeric route segment after the entity, when present.</param>
/// <param name="Path">The raw request path.</param>
/// <param name="StatusCode">The HTTP status returned (always 2xx).</param>
/// <param name="DurationMs">Server-side duration of the request.</param>
public record AuditLogDto(
    int Id,
    DateTime CreatedAt,
    int? UserId,
    string UserName,
    string Role,
    string Action,
    string Entity,
    int? EntityId,
    string Path,
    int StatusCode,
    int DurationMs);
