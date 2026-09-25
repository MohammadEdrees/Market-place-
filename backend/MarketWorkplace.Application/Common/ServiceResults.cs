using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MarketWorkplace.Application.Common;

/// <summary>
/// Result factories used by application services. The shapes mirror the <c>ControllerBase</c>
/// helpers (<c>Ok</c>, <c>NotFound</c>, <c>Problem</c>, …), so business rules defined in this
/// layer keep producing the exact HTTP responses the API contract promises.
/// </summary>
public static class ServiceResults
{
    public static OkObjectResult Ok(object? value) => new(value);

    /// <summary>201 with a Location header relative to <paramref name="actionName"/>.</summary>
    public static CreatedAtActionResult CreatedAtAction(string actionName, object? routeValues, object? value) =>
        new(actionName, null, routeValues, value);

    /// <summary>201 with a Location header pointing at <paramref name="uri"/> (mirrors <c>ControllerBase.Created</c>).</summary>
    public static CreatedResult Created(string? uri, object? value) => new(uri, value);

    public static UnauthorizedResult Unauthorized() => new();

    public static NotFoundResult NotFound() => new();

    public static NoContentResult NoContent() => new();

    public static ForbidResult Forbid() => new();

    public static ConflictResult Conflict() => new();

    /// <summary>RFC 7807 problem response (mirrors <c>ControllerBase.Problem</c>).</summary>
    public static ObjectResult Problem(
        string? title = null,
        string? detail = null,
        int statusCode = StatusCodes.Status500InternalServerError) =>
        new(new ProblemDetails { Title = title, Detail = detail, Status = statusCode })
        {
            StatusCode = statusCode,
        };
}
