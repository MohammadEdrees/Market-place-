using System.ComponentModel.DataAnnotations;

namespace MarketWorkplace.Application.Dtos;

/// <summary>Request body for placing an order: buy a product or reserve a service.</summary>
public record OrderInput
{
    /// <summary><c>Product</c> to buy or <c>Service</c> to reserve.</summary>
    [Required, StringLength(20)]
    public string Kind { get; init; } = "Product";

    /// <summary>Product to buy (required when Kind is <c>Product</c>).</summary>
    public int? ProductId { get; init; }

    /// <summary>Service to reserve (required when Kind is <c>Service</c>).</summary>
    public int? ServiceId { get; init; }
}

/// <summary>Status transition applied by <c>PUT /api/orders/{id}/status</c>.</summary>
public record OrderStatusInput
{
    /// <summary>New status: Processing, Confirmed, Completed, Cancelled, Reserved or Refunded.</summary>
    [Required, StringLength(40)]
    public string Status { get; init; } = string.Empty;
}
