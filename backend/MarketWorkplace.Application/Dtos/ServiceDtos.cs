using System.ComponentModel.DataAnnotations;

namespace MarketWorkplace.Application.Dtos;

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
