using MarketWorkplace.Application.Interfaces;
using MarketWorkplace.Domain.Entities;

namespace MarketWorkplace.Application.Services;

/// <summary>
/// Appends one entry to the audit trail and enforces the retention cap by
/// dropping the oldest rows once the table is full.
/// </summary>
/// <remarks>
/// Resolved from a scope created by <c>AuditLogMiddleware</c>, so the write runs on
/// its own <c>MarketDbContext</c>: a failing insert can never disturb — or be rolled
/// back with — the request's own unit of work.
/// </remarks>
public class AuditLogWriter(IRepository<AuditLog> auditLogs)
{
    /// <summary>How many entries the trail keeps; the oldest beyond this are pruned on write.</summary>
    public const int MaxEntries = 10_000;

    /// <summary>Records <paramref name="entry"/>, pruning the oldest rows first when at capacity.</summary>
    public void Write(AuditLog entry)
    {
        var count = auditLogs.QueryReadOnly().Count();
        if (count >= MaxEntries)
        {
            // Prune before inserting so one insert + one delete share a SaveChanges.
            var excess = count - MaxEntries + 1;
            var oldest = auditLogs.Query()
                .OrderBy(a => a.Id)
                .Take(excess)
                .ToList();
            auditLogs.RemoveRange(oldest);
        }

        auditLogs.Add(entry);
        auditLogs.SaveChanges();
    }
}
