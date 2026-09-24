using System.ComponentModel.DataAnnotations;

namespace MarketWorkplace.Api.Models;

/// <summary>A user of the dashboard or mobile app (passwords stored as PBKDF2 hashes).</summary>
public class User
{
    public int Id { get; set; }

    /// <summary>Unique sign-in address, e.g. <c>admin@marketplace.dev</c>.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Display name shown in the topbar and on public profiles.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>PBKDF2 hash produced by <c>Auth.PasswordHasher</c> — never the plain password.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Role carried in the token's <c>role</c> claim, e.g. <c>Admin</c> or <c>Provider</c>.</summary>
    public string Role { get; set; } = "Viewer";

    /// <summary>Platform the account belongs to: <c>Dashboard</c> (web) or <c>Mobile</c>.</summary>
    public string Type { get; set; } = "Dashboard";

    /// <summary>Contact phone shown on the public profile.</summary>
    public string? Phone { get; set; }

    /// <summary>Location (city / address) shown on the public profile.</summary>
    public string? Location { get; set; }

    /// <summary>Short bio or contact blurb shown on the public profile.</summary>
    public string? Bio { get; set; }
}

/// <summary>Credentials posted to <c>POST /api/auth/login</c>.</summary>
public record LoginRequest
{
    /// <summary>Email address of the account to sign in as.</summary>
    [Required, EmailAddress, StringLength(200)]
    public string Email { get; init; } = string.Empty;

    /// <summary>Plain-text password (verified against the stored hash).</summary>
    [Required, StringLength(100)]
    public string Password { get; init; } = string.Empty;
}

/// <summary>The signed-in user as returned by the API.</summary>
/// <param name="Id">User identifier.</param>
/// <param name="Email">Sign-in address.</param>
/// <param name="Name">Display name.</param>
/// <param name="Role">Role claim carried by the token.</param>
/// <param name="Type"><c>Dashboard</c> (web) or <c>Mobile</c> account.</param>
/// <param name="Phone">Contact phone, if set.</param>
/// <param name="Location">Location, if set.</param>
/// <param name="Bio">Short bio, if set.</param>
public record UserProfileDto(
    int Id,
    string Email,
    string Name,
    string Role,
    string Type,
    string? Phone,
    string? Location,
    string? Bio);

/// <summary>Payload for <c>POST /api/auth/register</c> — creates a mobile account and returns a token.</summary>
public record RegisterRequest
{
    /// <summary>Email address for the new account (must be unique).</summary>
    [Required, EmailAddress, StringLength(200)]
    public string Email { get; init; } = string.Empty;

    /// <summary>Plain-text password (stored as a PBKDF2 hash); at least 6 characters.</summary>
    [Required, MinLength(6), StringLength(100)]
    public string Password { get; init; } = string.Empty;

    /// <summary>Display name shown on profiles and order records.</summary>
    [Required, StringLength(120)]
    public string Name { get; init; } = string.Empty;

    /// <summary>Mobile account type: <c>Client</c> (default) or <c>Provider</c> (service provider / seller).</summary>
    [StringLength(40)]
    public string? Role { get; init; }

    /// <summary>Optional contact phone.</summary>
    [StringLength(40)]
    public string? Phone { get; init; }

    /// <summary>Optional location (city / address).</summary>
    [StringLength(120)]
    public string? Location { get; init; }
}

/// <summary>Contact/profile fields a user may edit about themselves (<c>PUT /api/users/me</c>).
/// Null leaves the field unchanged; send an empty string to clear it.</summary>
public record UserUpdateInput
{
    /// <summary>New display name.</summary>
    [StringLength(120)]
    public string? Name { get; init; }

    /// <summary>New contact phone.</summary>
    [StringLength(40)]
    public string? Phone { get; init; }

    /// <summary>New location.</summary>
    [StringLength(120)]
    public string? Location { get; init; }

    /// <summary>New bio.</summary>
    [StringLength(500)]
    public string? Bio { get; init; }
}

/// <summary>Result of a successful login.</summary>
/// <param name="Token">Bearer token to send as <c>Authorization: Bearer &lt;token&gt;</c>.</param>
/// <param name="ExpiresAt">UTC timestamp after which the token is rejected.</param>
/// <param name="User">Profile of the signed-in user.</param>
public record LoginResponse(string Token, DateTime ExpiresAt, UserProfileDto User);
