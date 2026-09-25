using System.ComponentModel.DataAnnotations;

namespace MarketWorkplace.Application.Dtos;

/// <summary>Request body for creating or updating a product. Server-generated fields are omitted.</summary>
public class ProductInput
{
    /// <summary>Display name.</summary>
    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Stock keeping unit.</summary>
    [Required, StringLength(40)]
    public string Sku { get; set; } = string.Empty;

    /// <summary>Category name; free text so new categories can be introduced.</summary>
    [Required, StringLength(60)]
    public string Category { get; set; } = string.Empty;

    /// <summary>Unit price in USD; must be zero or greater.</summary>
    [Range(0, 1_000_000)]
    public decimal Price { get; set; }

    /// <summary>Units in stock; must be zero or greater.</summary>
    [Range(0, 1_000_000)]
    public int Stock { get; set; }
}
