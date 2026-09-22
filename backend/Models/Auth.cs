using System.ComponentModel.DataAnnotations;

namespace MarketWorkplace.Api.Models;

/// <summary>A user of the dashboard; seeded in-memory (passwords stored as PBKDF2 hashes).</summary>
public class User
{
    public int Id { get; set; }

    /// <summary>Unique sign-in address, e.g. <c>admin@marketplace.dev</c>.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Display name shown in the topbar.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>PBKDF2 hash produced by <c>Auth.PasswordHasher</c> — never the plain password.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Role carried in the token's <c>role</c> claim, e.g. <c>Admin</c>.</summary>
    public string Role { get; set; } = "Viewer";
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
public record UserProfileDto(int Id, string Email, string Name, string Role);

/// <summary>Result of a successful login.</summary>
/// <param name="Token">Bearer token to send as <c>Authorization: Bearer &lt;token&gt;</c>.</param>
/// <param name="ExpiresAt">UTC timestamp after which the token is rejected.</param>
/// <param name="User">Profile of the signed-in user.</param>
public record LoginResponse(string Token, DateTime ExpiresAt, UserProfileDto User);
