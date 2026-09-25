using System.ComponentModel.DataAnnotations;

namespace MarketWorkplace.Application.Dtos;

/// <summary>Request body for creating a subscription.</summary>
public class SubscriptionCreateInput
{
    /// <summary>The subscribed account.</summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>Plan name: <c>Basic</c>, <c>Premium</c> or <c>Enterprise</c>.</summary>
    [Required, StringLength(30)]
    public string Plan { get; set; } = string.Empty;

    /// <summary>Monthly price in USD for this plan.</summary>
    [Range(0, 1_000_000)]
    public decimal Price { get; set; }

    /// <summary>Billing cadence: <c>Monthly</c> or <c>Yearly</c>.</summary>
    [Required, StringLength(10)]
    public string BillingCycle { get; set; } = "Monthly";

    /// <summary>When the subscription starts.</summary>
    public DateTime? StartsAt { get; set; }

    /// <summary>When it ends, or <c>null</c> for an open-ended plan.</summary>
    public DateTime? EndsAt { get; set; }

    /// <summary>Whether it renews automatically.</summary>
    public bool AutoRenew { get; set; }
}

/// <summary>Request body for updating an existing subscription.</summary>
public class SubscriptionUpdateInput
{
    /// <summary>Plan name: <c>Basic</c>, <c>Premium</c> or <c>Enterprise</c>.</summary>
    [Required, StringLength(30)]
    public string Plan { get; set; } = string.Empty;

    /// <summary>Monthly price in USD for this plan.</summary>
    [Range(0, 1_000_000)]
    public decimal Price { get; set; }

    /// <summary>Billing cadence: <c>Monthly</c> or <c>Yearly</c>.</summary>
    [Required, StringLength(10)]
    public string BillingCycle { get; set; } = "Monthly";

    /// <summary>Whether the subscription is currently usable.</summary>
    [Required, StringLength(10)]
    public string Status { get; set; } = "Active";

    /// <summary>When it ends, or <c>null</c> for an open-ended plan.</summary>
    public DateTime? EndsAt { get; set; }

    /// <summary>Whether it renews automatically.</summary>
    public bool AutoRenew { get; set; }
}

/// <summary>A subscription as returned by the subscription endpoints.</summary>
/// <param name="Id">Subscription identifier.</param>
/// <param name="UserId">The subscribed account.</param>
/// <param name="UserName">The account's display name (from <see cref="User"/>).</param>
/// <param name="UserEmail">The account's email.</param>
/// <param name="UserType"><c>Dashboard</c> or <c>Mobile</c>.</param>
/// <param name="Plan">Plan name.</param>
/// <param name="Price">Monthly price in USD.</param>
/// <param name="BillingCycle"><c>Monthly</c> or <c>Yearly</c>.</param>
/// <param name="Status"><c>Active</c>, <c>Inactive</c> or <c>Expired</c>.</param>
/// <param name="StartsAt">When it started.</param>
/// <param name="EndsAt">When it ends.</param>
/// <param name="AutoRenew">Whether it renews automatically.</param>
/// <param name="CreatedAt">When the record was created.</param>
public record SubscriptionDto(
    int Id,
    int UserId,
    string UserName,
    string UserEmail,
    string UserType,
    string Plan,
    decimal Price,
    string BillingCycle,
    string Status,
    DateTime StartsAt,
    DateTime? EndsAt,
    bool AutoRenew,
    DateTime CreatedAt);
