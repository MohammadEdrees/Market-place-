using System.Security.Claims;
using MarketWorkplace.Application.Common;
using MarketWorkplace.Application.Dtos;
using MarketWorkplace.Application.Interfaces;
using MarketWorkplace.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using static MarketWorkplace.Application.Common.ServiceResults;

namespace MarketWorkplace.Application.Services;

/// <summary>Read-only access to the audit trail (backs <c>AuditLogsController</c>).</summary>
/// <remarks>
/// Entries are written by the API's audit middleware, never through this service —
/// there is no create, update or delete endpoint, only the automatic retention cap.
/// Every method is limited to dashboard admins (<see cref="Access.IsAdmin"/>).
/// </remarks>
public class AuditLogsService(IRepository<AuditLog> auditLogs)
{
    /// <summary>Lists one page of the trail — newest first by default — with optional filters.</summary>
    /// <param name="caller">The signed-in account; non-admins are refused with <c>403</c>.</param>
    /// <param name="search">Free text over the actor, action, entity, role and path.</param>
    /// <param name="action">Exact action, e.g. <c>delete</c>.</param>
    /// <param name="entity">Exact route family, e.g. <c>products</c>.</param>
    /// <param name="userId">Only entries recorded for this account.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Rows per page (server-clamped).</param>
    /// <param name="sortBy">Whitelisted column; unknown values fall back to <c>createdAt</c>.</param>
    /// <param name="sortDir"><c>desc</c> by default, so the newest event is on top.</param>
    public ActionResult<PagedResponse<AuditLogDto>> GetAll(
        ClaimsPrincipal caller,
        string? search,
        string? action,
        string? entity,
        int? userId,
        int page = 1,
        int pageSize = Paging.DefaultPageSize,
        string? sortBy = null,
        string sortDir = "desc")
    {
        if (!Access.IsAdmin(caller))
        {
            return Forbid();
        }

        page = Paging.NormalizePage(page);
        pageSize = Paging.NormalizePageSize(pageSize);

        // Everything below stays an IQueryable: the trail is the one table that can
        // hold thousands of rows, so filtering, counting and paging run in SQL.
        IQueryable<AuditLog> filtered = auditLogs.QueryReadOnly();

        if (!string.IsNullOrWhiteSpace(search))
        {
            filtered = filtered.Where(a =>
                a.UserName.Contains(search) ||
                a.Action.Contains(search) ||
                a.Entity.Contains(search) ||
                a.Role.Contains(search) ||
                a.Path.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            filtered = filtered.Where(a => a.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(entity))
        {
            filtered = filtered.Where(a => a.Entity == entity);
        }

        if (userId is not null)
        {
            filtered = filtered.Where(a => a.UserId == userId);
        }

        var total = filtered.Count();
        var items = ApplySort(filtered, sortBy, sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .ThenBy(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(
                a.Id, a.CreatedAt, a.UserId, a.UserName, a.Role, a.Action,
                a.Entity, a.EntityId, a.Path, a.StatusCode, a.DurationMs))
            .ToList();

        return Ok(new PagedResponse<AuditLogDto>(items, total, page, pageSize));
    }

    /// <summary>Whitelisted sort keys; unknown/absent values give newest-first.</summary>
    private static IOrderedQueryable<AuditLog> ApplySort(IQueryable<AuditLog> rows, string? sortBy, bool desc) =>
        (sortBy ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "action" => SortBy(rows, a => a.Action, desc),
            "entity" => SortBy(rows, a => a.Entity, desc),
            "user" => SortBy(rows, a => a.UserName, desc),
            "path" => SortBy(rows, a => a.Path, desc),
            "duration" => SortBy(rows, a => a.DurationMs, desc),
            _ => SortBy(rows, a => a.CreatedAt, desc),
        };

    private static IOrderedQueryable<AuditLog> SortBy<TKey>(
        IQueryable<AuditLog> rows,
        System.Linq.Expressions.Expression<Func<AuditLog, TKey>> key,
        bool desc) =>
        desc ? rows.OrderByDescending(key) : rows.OrderBy(key);
}
