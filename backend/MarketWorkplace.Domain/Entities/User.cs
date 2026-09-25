using System.ComponentModel.DataAnnotations;

namespace MarketWorkplace.Domain.Entities;

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

    /// <summary>Foreign key of the assigned <see cref="Role"/>.</summary>
    public int RoleId { get; set; }

    /// <summary>The assigned role; its <c>Name</c> is carried in the token's <c>role</c> claim.</summary>
    public Role Role { get; set; } = null!;

    /// <summary>Platform the account belongs to: <c>Dashboard</c> (web) or <c>Mobile</c>.</summary>
    public string Type { get; set; } = "Dashboard";

    /// <summary>Contact phone shown on the public profile.</summary>
    public string? Phone { get; set; }

    /// <summary>Location (city / address) shown on the public profile.</summary>
    public string? Location { get; set; }

    /// <summary>Short bio or contact blurb shown on the public profile.</summary>
    public string? Bio { get; set; }

    /// <summary>Profile picture URL (absolute — prefixed with <c>BackendUrl</c> from appsettings), or <c>null</c>.</summary>
    public string? ImagePath { get; set; }

    /// <summary>When the account was created (UTC) — powers the dashboard's user statistics.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Subscriptions purchased by this account.</summary>
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
