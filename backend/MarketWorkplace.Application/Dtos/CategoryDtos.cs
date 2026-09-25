using System.ComponentModel.DataAnnotations;

namespace MarketWorkplace.Application.Dtos;

/// <summary>Request body for creating a category.</summary>
public class CategoryCreateInput
{
    /// <summary>Display name, e.g. <c>Audio</c>; unique within the kind (case-insensitive).</summary>
    [Required, StringLength(60)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Which catalogue it belongs to: <c>Product</c> or <c>Service</c>.</summary>
    [Required, StringLength(20)]
    public string Kind { get; set; } = string.Empty;
}

/// <summary>Request body for renaming a category; the kind never changes.</summary>
public class CategoryUpdateInput
{
    /// <summary>New display name; existing listings are renamed with it.</summary>
    [Required, StringLength(60)]
    public string Name { get; set; } = string.Empty;
}

/// <summary>A category as returned by the category endpoints.</summary>
/// <param name="Id">Category identifier.</param>
/// <param name="Name">Display name carried by product/service rows.</param>
/// <param name="Kind"><c>Product</c> or <c>Service</c>.</param>
/// <param name="ListingCount">How many listings currently use the category.</param>
/// <param name="CreatedAt">When the category was added.</param>
public record CategoryDto(int Id, string Name, string Kind, int ListingCount, DateTime CreatedAt);
