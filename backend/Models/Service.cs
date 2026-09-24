using System.ComponentModel.DataAnnotations;

namespace MarketWorkplace.Api.Models;

/// <summary>A service offered by a provider/seller that clients can reserve.</summary>
public class Service
{
    public int Id { get; set; }

    /// <summary>Provider / seller / dashboard admin who offers this service.</summary>
    public int ProviderId { get; set; }

    /// <summary>Display title, e.g. <c>Home Wi-Fi Setup</c>.</summary>
    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Long description of what is offered.</summary>
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Category used for grouping and filtering.</summary>
    [Required, StringLength(60)]
    public string Category { get; set; } = string.Empty;

    /// <summary>Cost of the service in USD.</summary>
    [Range(0, 1_000_000)]
    public decimal Cost { get; set; }

    /// <summary>How to reach the provider (phone / email / WhatsApp).</summary>
    [Required, StringLength(200)]
    public string ContactInfo { get; set; } = string.Empty;

    /// <summary>Service area or location.</summary>
    [Required, StringLength(120)]
    public string Location { get; set; } = string.Empty;

    /// <summary>Current offers as free text, e.g. <c>20% off the first booking</c>.</summary>
    [StringLength(500)]
    public string Offers { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gallery images stored under <c>wwwroot/images/services</c> (ordered by <c>SortOrder</c>).</summary>
    public List<ListingImage> Images { get; set; } = [];
}

/// <summary>Request body for creating or updating a service.</summary>
public class ServiceInput
{
    /// <summary>Display title.</summary>
    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Long description of what is offered.</summary>
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Category name; free text so new categories can be introduced.</summary>
    [Required, StringLength(60)]
    public string Category { get; set; } = string.Empty;

    /// <summary>Cost in USD; must be zero or greater.</summary>
    [Range(0, 1_000_000)]
    public decimal Cost { get; set; }

    /// <summary>How to reach the provider (phone / email / WhatsApp).</summary>
    [Required, StringLength(200)]
    public string ContactInfo { get; set; } = string.Empty;

    /// <summary>Service area or location.</summary>
    [Required, StringLength(120)]
    public string Location { get; set; } = string.Empty;

    /// <summary>Current offers as free text.</summary>
    [StringLength(500)]
    public string Offers { get; set; } = string.Empty;
}
