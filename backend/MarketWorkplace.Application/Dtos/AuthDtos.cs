using System.ComponentModel.DataAnnotations;

namespace MarketWorkplace.Application.Dtos;

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
/// <param name="ImagePath">Profile picture URL (absolute, from <c>BackendUrl</c> in appsettings), if set.</param>
public record UserProfileDto(
    int Id,
    string Email,
    string Name,
    string Role,
    string Type,
    string? Phone,
    string? Location,
    string? Bio,
    string? ImagePath);

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

/// <summary>Payload for <c>POST /api/users</c> — a dashboard admin creating any kind of account.</summary>
public record UserCreateInput
{
    /// <summary>Display name for the new account.</summary>
    [Required, StringLength(120)]
    public string Name { get; init; } = string.Empty;

    /// <summary>Email address (must be unique).</summary>
    [Required, EmailAddress, StringLength(200)]
    public string Email { get; init; } = string.Empty;

    /// <summary>Plain-text password (stored as a PBKDF2 hash); at least 6 characters.</summary>
    [Required, MinLength(6), StringLength(100)]
    public string Password { get; init; } = string.Empty;

    /// <summary>Role: <c>SuperAdmin</c>, <c>Admin</c>, <c>Manager</c>, <c>Viewer</c>, <c>Provider</c> or <c>Client</c>.
    /// The platform is derived from it — <c>Provider</c>/<c>Client</c> become <c>Mobile</c>, everything else <c>Dashboard</c>.</summary>
    [Required, StringLength(40)]
    public string Role { get; init; } = string.Empty;

    /// <summary>Optional contact phone.</summary>
    [StringLength(40)]
    public string? Phone { get; init; }

    /// <summary>Optional location (city / address).</summary>
    [StringLength(120)]
    public string? Location { get; init; }

    /// <summary>Optional bio.</summary>
    [StringLength(500)]
    public string? Bio { get; init; }
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

/// <summary>Payload for <c>PUT /api/users/{id}</c> — a dashboard admin editing any account.
/// Unlike <see cref="UserUpdateInput"/> (self-service) this may change the email, the role
/// (platform is re-derived) and optionally reset the password.</summary>
public record UserEditInput
{
    /// <summary>New display name (required).</summary>
    [Required, StringLength(120)]
    public string Name { get; init; } = string.Empty;

    /// <summary>New email address (must be unique).</summary>
    [Required, EmailAddress, StringLength(200)]
    public string Email { get; init; } = string.Empty;

    /// <summary>New role: <c>SuperAdmin</c>, <c>Admin</c>, <c>Manager</c>, <c>Viewer</c>,
    /// <c>Provider</c> or <c>Client</c>; the platform (<c>Dashboard</c>/<c>Mobile</c>) is re-derived.</summary>
    [Required, StringLength(40)]
    public string Role { get; init; } = string.Empty;

    /// <summary>Optional new password (min 6 chars); null or empty keeps the current one.</summary>
    [StringLength(100)]
    public string? Password { get; init; }

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
