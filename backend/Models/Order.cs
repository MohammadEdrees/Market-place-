using System.ComponentModel.DataAnnotations;

namespace MarketWorkplace.Api.Models;

/// <summary>An order: either a product purchase or a service reservation.</summary>
public class Order
{
    public int Id { get; set; }
    public string Customer { get; set; } = string.Empty;

    /// <summary>Product or service name (denormalized for display).</summary>
    public string Product { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Status { get; set; } = "Completed";
    public DateTime Date { get; set; }

    /// <summary><c>Product</c> (buy) or <c>Service</c> (reserve).</summary>
    public string Kind { get; set; } = "Product";

    /// <summary>Purchased product when <see cref="Kind"/> is <c>Product</c>.</summary>
    public int? ProductId { get; set; }

    /// <summary>Reserved service when <see cref="Kind"/> is <c>Service</c>.</summary>
    public int? ServiceId { get; set; }

    /// <summary>Buyer placing the order; <c>null</c> for legacy/demo history rows.</summary>
    public int? BuyerId { get; set; }
}

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
