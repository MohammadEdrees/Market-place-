using System.Diagnostics;
using System.Text.Json;
using MarketWorkplace.Application.Common;
using MarketWorkplace.Application.Services;
using MarketWorkplace.Domain.Entities;

namespace MarketWorkplace.Api.Middleware;

/// <summary>
/// Records every successful mutating API call (<c>POST</c>/<c>PUT</c>/<c>PATCH</c>/<c>DELETE</c>
/// under <c>/api/</c> that answers 2xx) as one <see cref="AuditLog"/> row, so the trail stays
/// complete as the API grows — no endpoint has to remember to log itself.
/// </summary>
/// <remarks>
/// Placed after authentication so the JWT claims identify the actor, and after the response
/// completes so the write cannot slow down or roll back the call being recorded. Failures are
/// swallowed with a warning: an unavailable audit store must never fail a user's request.
/// </remarks>
public sealed class AuditLogMiddleware(
    RequestDelegate next,
    IServiceScopeFactory scopeFactory,
    ILogger<AuditLogMiddleware> logger)
{
    /// <summary>Only verbs that change state are worth recording.</summary>
    private static readonly string[] MutableMethods = ["POST", "PUT", "PATCH", "DELETE"];

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (!MutableMethods.Contains(context.Request.Method, StringComparer.OrdinalIgnoreCase) ||
            !path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        // Sign-in and register carry the actor only inside the request body, so buffer it —
        // the e-mail is read back after the action has run.
        var isAuthCall = path.StartsWith("/api/auth/", StringComparison.OrdinalIgnoreCase);
        if (isAuthCall)
        {
            context.Request.EnableBuffering();
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
        }

        var statusCode = context.Response.StatusCode;
        if (statusCode is < 200 or >= 300)
        {
            // Rejected calls (400/401/403/404/409) are not trail entries: the log answers
            // "what changed", and probes from list pages would otherwise bury real events.
            return;
        }

        try
        {
            var entry = Build(context, path, stopwatch.Elapsed, statusCode, isAuthCall);

            // Own scope, own DbContext — see the remarks on AuditLogWriter.
            using var scope = scopeFactory.CreateScope();
            scope.ServiceProvider.GetRequiredService<AuditLogWriter>().Write(entry);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Audit entry for {Method} {Path} was not recorded", context.Request.Method, path);
        }
    }

    /// <summary>Turns the finished request into a trail entry.</summary>
    private static AuditLog Build(HttpContext context, string path, TimeSpan elapsed, int statusCode, bool isAuthCall)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var entity = segments.Length > 1 ? segments[1].ToLowerInvariant() : "unknown";

        // The target id is the first numeric route segment (`/api/products/12/images/7` → 12).
        int? entityId = null;
        for (var i = 2; i < segments.Length; i++)
        {
            if (int.TryParse(segments[i], out var id))
            {
                entityId = id;
                break;
            }
        }

        return new AuditLog
        {
            CreatedAt = DateTime.UtcNow,
            UserId = Access.UserId(context.User),
            UserName = ActorName(context, isAuthCall),
            Role = Access.Role(context.User),
            Action = ActionFor(context.Request.Method, path),
            Entity = entity,
            EntityId = entityId,
            Path = path,
            StatusCode = statusCode,
            DurationMs = (int)elapsed.TotalMilliseconds,
        };
    }

    /// <summary>
    /// Who did it: the JWT name when signed in, otherwise the e-mail carried by the
    /// anonymous sign-in/register body.
    /// </summary>
    private static string ActorName(HttpContext context, bool isAuthCall)
    {
        var name = context.User.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        return isAuthCall ? ReadEmail(context.Request) ?? string.Empty : string.Empty;
    }

    /// <summary>Reads <c>email</c> from a buffered JSON body; returns <c>null</c> when absent.</summary>
    private static string? ReadEmail(HttpRequest request)
    {
        try
        {
            if (request.Body.CanSeek)
            {
                request.Body.Seek(0, SeekOrigin.Begin);
            }

            using var document = JsonDocument.Parse(request.Body);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("email", out var email) &&
                email.ValueKind == JsonValueKind.String)
            {
                return email.GetString();
            }
        }
        catch (Exception)
        {
            // Not JSON, empty or already consumed — the entry still records the call itself.
        }
        finally
        {
            if (request.Body.CanSeek)
            {
                request.Body.Seek(0, SeekOrigin.Begin);
            }
        }

        return null;
    }

    /// <summary>Maps the verb (and a couple of well-known sub-routes) onto the trail's action.</summary>
    private static string ActionFor(string method, string path)
    {
        if (path.Contains("/auth/login", StringComparison.OrdinalIgnoreCase)) return "login";
        if (path.Contains("/auth/register", StringComparison.OrdinalIgnoreCase)) return "register";
        if (path.Contains("/backup/restore", StringComparison.OrdinalIgnoreCase)) return "restore";

        return method.ToUpperInvariant() switch
        {
            "PUT" or "PATCH" => "update",
            "DELETE" => "delete",
            _ => "create",
        };
    }
}
