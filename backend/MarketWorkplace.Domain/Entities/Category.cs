using System.ComponentModel.DataAnnotations;

namespace MarketWorkplace.Domain.Entities;

/// <summary>
/// A managed category name for the product or service catalogue. The filter chips, the
/// dashboard forms and the mobile category dropdowns all read this list; it is kept in
/// sync with the categories already typed onto <see cref="Product"/>/<see cref="Service"/>
/// rows at startup and whenever a listing is saved.
/// </summary>
public class Category
{
    /// <summary>Kind used by product categories.</summary>
    public const string ProductKind = "Product";

    /// <summary>Kind used by service categories.</summary>
    public const string ServiceKind = "Service";

    public int Id { get; set; }

    /// <summary>Display name, e.g. <c>Audio</c>; unique within <see cref="Kind"/> (case-insensitive).</summary>
    [Required, StringLength(60)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Which catalogue the category belongs to: <c>Product</c> or <c>Service</c>.</summary>
    [Required, StringLength(20)]
    public string Kind { get; set; } = ProductKind;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
