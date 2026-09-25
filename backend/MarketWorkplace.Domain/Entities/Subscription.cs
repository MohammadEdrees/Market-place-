using System.ComponentModel.DataAnnotations;

namespace MarketWorkplace.Domain.Entities;

/// <summary>A plan a user is subscribed to — powers the dashboard Subscriptions page
/// and is shown on the linked mobile user's profile.</summary>
public class Subscription
{
    /// <summary>Billing cadences supported by the platform.</summary>
    public const string Monthly = "Monthly";
    public const string Yearly = "Yearly";

    /// <summary>Known plans.</summary>
    public const string BasicPlan = "Basic";
    public const string PremiumPlan = "Premium";
    public const string EnterprisePlan = "Enterprise";

    /// <summary>Active subscription states.</summary>
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public const string Expired = "Expired";

    public int Id { get; set; }

    /// <summary>Foreign key of the subscribed <see cref="User"/>.</summary>
    public int UserId { get; set; }

    /// <summary>The subscribed account (mobile users primarily, dashboard admins too).</summary>
    public User User { get; set; } = null!;

    /// <summary>Plan name, e.g. <c>Basic</c>, <c>Premium</c> or <c>Enterprise</c>.</summary>
    [Required, StringLength(30)]
    public string Plan { get; set; } = BasicPlan;

    /// <summary>Monthly price in USD for this plan.</summary>
    [Range(0, 1_000_000)]
    public decimal Price { get; set; }

    /// <summary>How often the plan bills.</summary>
    [Required, StringLength(10)]
    public string BillingCycle { get; set; } = Monthly;

    /// <summary>Whether the subscription is currently usable.</summary>
    [Required, StringLength(10)]
    public string Status { get; set; } = Active;

    /// <summary>When the subscription started.</summary>
    public DateTime StartsAt { get; set; } = DateTime.UtcNow;

    /// <summary>When the subscription ends, or <c>null</c> while it is open-ended.</summary>
    public DateTime? EndsAt { get; set; }

    /// <summary>Whether it renews automatically at the end of the cycle.</summary>
    public bool AutoRenew { get; set; }

    /// <summary>When the subscription was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
