namespace MarketWorkplace.Api.Models.Dashboard;

/// <summary>A single KPI tile on the Overview page.</summary>
/// <param name="Key">Stable identifier used by the client for tracking, e.g. <c>revenue</c>.</param>
/// <param name="Label">Human-readable label, e.g. <c>Revenue</c>.</param>
/// <param name="Value">Pre-formatted display value, e.g. <c>$11,654</c>.</param>
/// <param name="Delta">Change versus the previous 30-day window, formatted as a percentage.</param>
/// <param name="IsUp">Whether the change should be presented as positive.</param>
/// <param name="Icon">PrimeIcons name without the <c>pi pi-</c> prefix.</param>
public record MetricCardDto(
    string Key,
    string Label,
    string Value,
    string Delta,
    bool IsUp,
    string Icon);

/// <summary>One month of chart data.</summary>
/// <param name="Label">Short month name, e.g. <c>Oct</c>.</param>
/// <param name="Revenue">Total revenue for the month in USD.</param>
/// <param name="Orders">Number of orders placed in the month.</param>
public record TrendPointDto(string Label, decimal Revenue, int Orders);

/// <summary>Revenue contribution of one category.</summary>
/// <param name="Label">Category name.</param>
/// <param name="Value">Total revenue for the category in USD.</param>
public record CategoryShareDto(string Label, decimal Value);

/// <summary>A row in the Recent orders table.</summary>
/// <param name="Id">Order identifier.</param>
/// <param name="Customer">Customer or account name.</param>
/// <param name="Product">Product purchased.</param>
/// <param name="Total">Order total in USD.</param>
/// <param name="Status">One of <c>Completed</c>, <c>Processing</c> or <c>Refunded</c>.</param>
/// <param name="Date">When the order was placed (UTC).</param>
public record RecentOrderDto(
    int Id,
    string Customer,
    string Product,
    decimal Total,
    string Status,
    DateTime Date);

/// <summary>Payload returned by <c>GET /api/dashboard</c>.</summary>
/// <param name="Metrics">KPI tiles for the Overview page.</param>
/// <param name="RevenueTrend">Twelve months of revenue and order counts.</param>
/// <param name="SalesByCategory">Revenue split by category, largest first.</param>
/// <param name="RecentOrders">The six most recent orders.</param>
/// <param name="LowStockCount">Products at or below the low-stock threshold (15 units).</param>
public record DashboardResponse(
    IReadOnlyList<MetricCardDto> Metrics,
    IReadOnlyList<TrendPointDto> RevenueTrend,
    IReadOnlyList<CategoryShareDto> SalesByCategory,
    IReadOnlyList<RecentOrderDto> RecentOrders,
    int LowStockCount);
