using System.ComponentModel.DataAnnotations;

namespace MarketWorkplace.Domain.Entities;

/// <summary>A catalogue item rendered as a row on the Products page.</summary>
public class Product
{
    public int Id { get; set; }

    /// <summary>Display name, e.g. <c>Aurora Wireless Headset</c>.</summary>
    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Stock keeping unit, unique in practice (not enforced).</summary>
    [Required, StringLength(40)]
    public string Sku { get; set; } = string.Empty;

    /// <summary>Category used for grouping and filtering.</summary>
    [Required, StringLength(60)]
    public string Category { get; set; } = string.Empty;

    /// <summary>Unit price in USD.</summary>
    [Range(0, 1_000_000)]
    public decimal Price { get; set; }

    /// <summary>Units currently in stock.</summary>
    [Range(0, 1_000_000)]
    public int Stock { get; set; }

    /// <summary>Units sold across all time.</summary>
    [Range(0, 1_000_000)]
    public int Sold { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Seller / provider / dashboard admin who owns this listing; <c>null</c> for platform demo items.</summary>
    public int? SellerId { get; set; }

    /// <summary>Gallery images stored under <c>wwwroot/images/products</c> (ordered by <c>SortOrder</c>).</summary>
    public List<ListingImage> Images { get; set; } = [];

    /// <summary>Derived stock status shown in the dashboard table: <c>Active</c>, <c>Low stock</c> (≤ 15) or <c>Out of stock</c>.</summary>
    public string Status => Stock == 0 ? "Out of stock" : Stock <= 15 ? "Low stock" : "Active";
}
