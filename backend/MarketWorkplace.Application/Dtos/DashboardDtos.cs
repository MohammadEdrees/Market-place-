namespace MarketWorkplace.Application.Dtos;

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

/// <summary>One line of the Top products ranking (all-time, best revenue first).</summary>
/// <param name="Name">Product or service name as ordered.</param>
/// <param name="Kind"><c>Product</c> or <c>Service</c>.</param>
/// <param name="Orders">Number of orders placed for it.</param>
/// <param name="Revenue">Total revenue in USD.</param>
public record TopProductDto(string Name, string Kind, int Orders, decimal Revenue);

/// <summary>One slice of the all-time orders-by-status breakdown.</summary>
/// <param name="Status">Order status, e.g. <c>Completed</c>.</param>
/// <param name="Count">Orders in that status.</param>
/// <param name="Share">Percentage of all orders, one decimal.</param>
public record StatusShareDto(string Status, int Count, decimal Share);

/// <summary>Audience figures computed from the accounts table.</summary>
/// <param name="Total">All accounts.</param>
/// <param name="Clients">Accounts with the <c>Client</c> role.</param>
/// <param name="Providers">Accounts with the <c>Provider</c> role.</param>
/// <param name="Mobile">Accounts on the mobile platform.</param>
/// <param name="Dashboard">Accounts on the dashboard platform.</param>
/// <param name="NewLast30">Accounts created in the last 30 days.</param>
public record UserStatsDto(
    int Total,
    int Clients,
    int Providers,
    int Mobile,
    int Dashboard,
    int NewLast30);

/// <summary>Payload returned by <c>GET /api/dashboard</c>.</summary>
/// <param name="Metrics">KPI tiles for the Overview page.</param>
/// <param name="RevenueTrend">Twelve months of revenue and order counts.</param>
/// <param name="SalesByCategory">Revenue split by category, largest first.</param>
/// <param name="RecentOrders">The six most recent orders.</param>
/// <param name="LowStockCount">Products at or below the low-stock threshold (15 units).</param>
/// <param name="TopProducts">Best five product/service lines by all-time revenue.</param>
/// <param name="OrdersByStatus">All orders grouped by status, largest first.</param>
/// <param name="UserStats">Account totals and new signups for the Users panel.</param>
public record DashboardResponse(
    IReadOnlyList<MetricCardDto> Metrics,
    IReadOnlyList<TrendPointDto> RevenueTrend,
    IReadOnlyList<CategoryShareDto> SalesByCategory,
    IReadOnlyList<RecentOrderDto> RecentOrders,
    int LowStockCount,
    IReadOnlyList<TopProductDto> TopProducts,
    IReadOnlyList<StatusShareDto> OrdersByStatus,
    UserStatsDto UserStats);
