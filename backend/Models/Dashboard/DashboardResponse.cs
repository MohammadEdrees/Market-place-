namespace MarketWorkplace.Api.Models.Dashboard;

public record MetricCardDto(
    string Key,
    string Label,
    string Value,
    string Delta,
    bool IsUp,
    string Icon);

public record TrendPointDto(string Label, decimal Revenue, int Orders);

public record CategoryShareDto(string Label, decimal Value);

public record RecentOrderDto(
    int Id,
    string Customer,
    string Product,
    decimal Total,
    string Status,
    DateTime Date);

public record DashboardResponse(
    IReadOnlyList<MetricCardDto> Metrics,
    IReadOnlyList<TrendPointDto> RevenueTrend,
    IReadOnlyList<CategoryShareDto> SalesByCategory,
    IReadOnlyList<RecentOrderDto> RecentOrders,
    int LowStockCount);
