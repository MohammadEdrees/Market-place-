using System.ComponentModel.DataAnnotations;

namespace MarketWorkplace.Api.Models;

/// <summary>One image in a product or service gallery, stored under <c>wwwroot/images</c>.</summary>
public class ListingImage
{
    public int Id { get; set; }

    /// <summary>Web path served by the static-files middleware, e.g. <c>/images/products/ab12….png</c>.</summary>
    [Required, StringLength(300)]
    public string Path { get; set; } = string.Empty;

    /// <summary>Product this image belongs to; <c>null</c> for service images.</summary>
    public int? ProductId { get; set; }

    /// <summary>Service this image belongs to; <c>null</c> for product images.</summary>
    public int? ServiceId { get; set; }

    /// <summary>Display order within the listing's gallery.</summary>
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
