using System.Security.Claims;

namespace MarketWorkplace.Api.Auth;

/// <summary>Role and ownership rules shared by the marketplace controllers.</summary>
/// <remarks>
/// Listing (adding products/services) is allowed for mobile <c>Provider</c> users
/// (service provider / seller) and dashboard admins (<c>SuperAdmin</c>, <c>Admin</c>,
/// <c>Manager</c>). Mobile <c>Client</c>s and dashboard <c>Viewer</c>s may only browse,
/// buy and reserve.
/// </remarks>
public static class Access
{
    /// <summary>Roles allowed to create products and services.</summary>
    private static readonly string[] ListingRoles = ["SuperAdmin", "Admin", "Manager", "Provider"];

    /// <summary>Dashboard roles that manage every listing and order.</summary>
    private static readonly string[] AdminRoles = ["SuperAdmin", "Admin", "Manager"];

    /// <summary>The caller's user id from the JWT <c>sub</c> claim, or <c>null</c> when absent.</summary>
    public static int? UserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirst("sub")?.Value;
        return int.TryParse(sub, out var id) ? id : null;
    }

    /// <summary>The caller's role from the JWT <c>role</c> claim.</summary>
    public static string Role(ClaimsPrincipal user) => user.FindFirst("role")?.Value ?? string.Empty;

    /// <summary>True when the caller may create listings (products/services).</summary>
    public static bool CanList(ClaimsPrincipal user) => ListingRoles.Contains(Role(user));

    /// <summary>True for dashboard admins, who may edit any listing and manage any order.</summary>
    public static bool IsAdmin(ClaimsPrincipal user) => AdminRoles.Contains(Role(user));
}
