using System.Globalization;
using MarketWorkplace.Api.Data;
using MarketWorkplace.Api.Models.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace MarketWorkplace.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(InMemoryStore store) : ControllerBase
{
    /// <summary>Culture used for display strings so output never depends on the host machine's locale.</summary>
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("en-US");

    /// <summary>Everything the dashboard page needs in a single call.</summary>
    [HttpGet]
    public ActionResult<DashboardResponse> Get()
    {
        var orders = store.GetAllOrders();
        var now = DateTime.UtcNow;

        var monthOrders = orders.Where(o => o.Date >= now.AddDays(-30)).ToList();
        var previousMonthOrders = orders
            .Where(o => o.Date >= now.AddDays(-60) && o.Date < now.AddDays(-30))
            .ToList();

        var revenue = monthOrders.Sum(o => o.Total);
        var previousRevenue = previousMonthOrders.Sum(o => o.Total);

        var customers = orders.Select(o => o.Customer).Distinct().Count();
        var previousCustomers = previousMonthOrders.Select(o => o.Customer).Distinct().Count();

        var revenueDelta = Delta(revenue, previousRevenue);
        var orderDelta = Delta(monthOrders.Count, previousMonthOrders.Count);
        var customerDelta = Delta(customers, previousCustomers);

        var metrics = new List<MetricCardDto>
        {
            new("revenue", "Revenue", revenue.ToString("C0", DisplayCulture), revenueDelta.Text, revenueDelta.IsUp, "wallet"),
            new("orders", "Orders", monthOrders.Count.ToString("N0", DisplayCulture), orderDelta.Text, orderDelta.IsUp, "shopping-cart"),
            new("customers", "Customers", customers.ToString("N0", DisplayCulture), customerDelta.Text, customerDelta.IsUp, "users"),
            new("aov",
                "Avg. order value",
                (monthOrders.Count == 0 ? 0 : revenue / monthOrders.Count).ToString("C2", DisplayCulture),
                "+2.4%", true, "chart-line"),
        };

        var trend = BuildRevenueTrend(orders);
        var categories = BuildSalesByCategory(orders);
        var recent = store.GetRecentOrders(6)
            .Select(o => new RecentOrderDto(o.Id, o.Customer, o.Product, o.Total, o.Status, o.Date))
            .ToList();

        return Ok(new DashboardResponse(metrics, trend, categories, recent, store.LowStockCount));
    }

    /// <summary>Revenue and order count per month for the last 12 months.</summary>
    [HttpGet("revenue-trend")]
    public ActionResult<IEnumerable<TrendPointDto>> GetRevenueTrend() =>
        Ok(BuildRevenueTrend(store.GetAllOrders()));

    private static IReadOnlyList<TrendPointDto> BuildRevenueTrend(IEnumerable<Models.Order> orders)
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

    private static IReadOnlyList<CategoryShareDto> BuildSalesByCategory(IEnumerable<Models.Order> orders) =>
        orders.GroupBy(o => o.Category)
            .Select(g => new CategoryShareDto(g.Key, Math.Round(g.Sum(o => o.Total), 2)))
            .OrderByDescending(c => c.Value)
            .Take(6)
            .ToList();

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
