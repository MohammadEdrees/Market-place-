using System.Globalization;
using MarketWorkplace.Application.Dtos;
using MarketWorkplace.Application.Interfaces;
using MarketWorkplace.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using static MarketWorkplace.Application.Common.ServiceResults;

namespace MarketWorkplace.Application.Services;

/// <summary>Aggregated figures for the dashboard's Overview page (backs <c>DashboardController</c>).</summary>
/// <remarks>
/// Every number here is computed live from the database — KPI deltas compare the last
/// 30 days against the previous window, the trend/category/top-product panels group the
/// order history, and the audience figures come from the accounts table. Nothing is cached
/// or stubbed.
/// </remarks>
public class DashboardService(
    IRepository<Order> orderList,
    IRepository<Product> products,
    IRepository<User> users,
    IRepository<Role> roles)
{
    /// <summary>Culture used for display strings so output never depends on the host machine's locale.</summary>
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("en-US");

    /// <summary>Everything the dashboard page needs in a single call.</summary>
    public ActionResult<DashboardResponse> Get()
    {
        var orders = orderList.QueryReadOnly().ToList();
        var now = DateTime.UtcNow;

        var monthOrders = orders.Where(o => o.Date >= now.AddDays(-30)).ToList();
        var previousMonthOrders = orders
            .Where(o => o.Date >= now.AddDays(-60) && o.Date < now.AddDays(-30))
            .ToList();

        var revenue = monthOrders.Sum(o => o.Total);
        var previousRevenue = previousMonthOrders.Sum(o => o.Total);

        var customers = monthOrders.Select(o => o.Customer).Distinct().Count();
        var previousCustomers = previousMonthOrders.Select(o => o.Customer).Distinct().Count();

        // Average order value, current vs. previous window (was a fixed "+2.4%" before).
        var aov = monthOrders.Count == 0 ? 0 : revenue / monthOrders.Count;
        var previousAov = previousMonthOrders.Count == 0 ? 0 : previousRevenue / previousMonthOrders.Count;

        var revenueDelta = Delta(revenue, previousRevenue);
        var orderDelta = Delta(monthOrders.Count, previousMonthOrders.Count);
        var customerDelta = Delta(customers, previousCustomers);
        var aovDelta = Delta(aov, previousAov);

        var metrics = new List<MetricCardDto>
        {
            new("revenue", "Revenue", revenue.ToString("C0", DisplayCulture), revenueDelta.Text, revenueDelta.IsUp, "wallet"),
            new("orders", "Orders", monthOrders.Count.ToString("N0", DisplayCulture), orderDelta.Text, orderDelta.IsUp, "shopping-cart"),
            new("customers", "Customers", customers.ToString("N0", DisplayCulture), customerDelta.Text, customerDelta.IsUp, "users"),
            new("aov",
                "Avg. order value",
                aov.ToString("C2", DisplayCulture),
                aovDelta.Text,
                aovDelta.IsUp,
                "chart-line"),
        };

        var trend = BuildRevenueTrend(orders);
        var categories = BuildSalesByCategory(orders);
        var recent = orders
            .OrderByDescending(o => o.Date)
            .Take(6)
            .Select(o => new RecentOrderDto(o.Id, o.Customer, o.Product, o.Total, o.Status, o.Date))
            .ToList();

        var lowStockCount = products.QueryReadOnly().Count(p => p.Stock <= 15);

        var topProducts = BuildTopProducts(orders);
        var ordersByStatus = BuildOrdersByStatus(orders);
        var userStats = BuildUserStats(now);

        return Ok(new DashboardResponse(
            metrics, trend, categories, recent, lowStockCount, topProducts, ordersByStatus, userStats));
    }

    /// <summary>Revenue and order count per month for the last 12 months.</summary>
    public ActionResult<IEnumerable<TrendPointDto>> GetRevenueTrend() =>
        Ok(BuildRevenueTrend(orderList.QueryReadOnly().ToList()));

    private static IReadOnlyList<TrendPointDto> BuildRevenueTrend(IEnumerable<Order> orders)
    {
        var now = DateTime.UtcNow;
        var points = new List<TrendPointDto>();

        for (var monthsBack = 11; monthsBack >= 0; monthsBack--)
        {
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-monthsBack);
            var monthEnd = monthStart.AddMonths(1);

            var monthOrders = orders
                .Where(o => o.Date >= monthStart && o.Date < monthEnd)
                .ToList();

            points.Add(new TrendPointDto(
                monthStart.ToString("MMM", DisplayCulture),
                Math.Round(monthOrders.Sum(o => o.Total), 2),
                monthOrders.Count));
        }

        return points;
    }

    private static IReadOnlyList<CategoryShareDto> BuildSalesByCategory(IEnumerable<Order> orders) =>
        orders.GroupBy(o => o.Category)
            .Select(g => new CategoryShareDto(g.Key, Math.Round(g.Sum(o => o.Total), 2)))
            .OrderByDescending(c => c.Value)
            .Take(6)
            .ToList();

    /// <summary>Best five product/service lines by all-time revenue, with their order counts.</summary>
    private static IReadOnlyList<TopProductDto> BuildTopProducts(IEnumerable<Order> orders) =>
        orders.GroupBy(o => new { o.Product, o.Kind })
            .Select(g => new TopProductDto(
                g.Key.Product,
                g.Key.Kind,
                g.Count(),
                Math.Round(g.Sum(o => o.Total), 2)))
            .OrderByDescending(p => p.Revenue)
            .Take(5)
            .ToList();

    /// <summary>All-time order counts per status plus each status's share of the total.</summary>
    private static IReadOnlyList<StatusShareDto> BuildOrdersByStatus(IReadOnlyList<Order> orders)
    {
        var total = orders.Count;
        return orders.GroupBy(o => o.Status)
            .Select(g => new StatusShareDto(
                g.Key,
                g.Count(),
                total == 0 ? 0 : Math.Round(g.Count() * 100m / total, 1)))
            .OrderByDescending(s => s.Count)
            .ToList();
    }

    /// <summary>Account totals by role/platform plus signups in the last 30 days.</summary>
    private UserStatsDto BuildUserStats(DateTime now)
    {
        var allUsers = users.QueryReadOnly().ToList();
        var roleNames = roles.QueryReadOnly().ToDictionary(r => r.Id, r => r.Name);

        int CountRole(string roleName) =>
            allUsers.Count(u => roleNames.TryGetValue(u.RoleId, out var name) && name == roleName);

        return new UserStatsDto(
            allUsers.Count,
            CountRole("Client"),
            CountRole("Provider"),
            allUsers.Count(u => u.Type == "Mobile"),
            allUsers.Count(u => u.Type == "Dashboard"),
            allUsers.Count(u => u.CreatedAt >= now.AddDays(-30)));
    }

    private static (string Text, bool IsUp) Delta(decimal current, decimal previous)
    {
        if (previous == 0)
        {
            return ("+0.0%", true);
        }

        var delta = (current - previous) / previous * 100;
        return ($"{(delta >= 0 ? "+" : "")}{delta:F1}%", delta >= 0);
    }
}
