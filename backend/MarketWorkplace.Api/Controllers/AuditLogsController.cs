using MarketWorkplace.Application.Dtos;
using MarketWorkplace.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketWorkplace.Api.Controllers;

/// <summary>
/// The audit trail: one row per successful mutating call the API has served, written by
/// <c>AuditLogMiddleware</c> rather than by any endpoint, so it stays complete as the API grows.
/// </summary>
/// <remarks>
/// <b>Read-only and admin-only.</b> There is no create/update/delete here — entries appear on
/// their own and the oldest are pruned automatically once the trail reaches its retention cap.
/// The trail is deliberately excluded from backup/restore: it is operational history, not data.
/// </remarks>
[ApiController]
[Authorize]
[Route("api/auditlogs")]
[Tags("Audit log")]
[Produces("application/json")]
public class AuditLogsController(AuditLogsService auditLogsService) : ControllerBase
{
    /// <summary>One page of the trail, newest first (admin callers only).</summary>
    /// <param name="search">Free text over the actor, action, entity, role and path.</param>
    /// <param name="action">Exact action: <c>create</c>, <c>update</c>, <c>delete</c>, <c>login</c>, <c>register</c> or <c>restore</c>.</param>
    /// <param name="entity">Exact route family, e.g. <c>products</c>.</param>
    /// <param name="userId">Only entries recorded for this account.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Rows per page (server-clamped to 100).</param>
    /// <param name="sortBy"><c>createdAt</c> (default), <c>action</c>, <c>entity</c>, <c>user</c>, <c>path</c> or <c>duration</c>.</param>
    /// <param name="sortDir"><c>desc</c> by default.</param>
    /// <returns>The matching entries, or <c>403</c> for non-admin callers.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<PagedResponse<AuditLogDto>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? action,
        [FromQuery] string? entity,
        [FromQuery] int? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paging.DefaultPageSize,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDir = "desc") =>
        auditLogsService.GetAll(User, search, action, entity, userId, page, pageSize, sortBy, sortDir);
}
