using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Areas.Admin.Pages;

public class IndexModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IRepository<Order> _orderRepository;

    public IndexModel(IOrderService orderService, IRepository<Order> orderRepository)
    {
        _orderService = orderService;
        _orderRepository = orderRepository;
    }

    public (int OrderCount, decimal Revenue, decimal AvgOrderValue, int LowStockCount) Stats { get; private set; }
    public IReadOnlyList<Order> RecentOrders { get; private set; } = [];
    public IReadOnlyList<ChartPoint> RevenueChart { get; private set; } = [];
    public decimal MaxDailyRevenue { get; private set; }

    public record ChartPoint(DateTime Date, decimal Revenue);

    public async Task OnGetAsync()
    {
        Stats = await _orderService.GetDashboardStatsAsync();

        var orders = await _orderRepository.GetAllAsync();
        RecentOrders = orders
            .OrderByDescending(o => o.PlacedAt)
            .Take(8)
            .ToList();

        var today = DateTime.UtcNow.Date;
        RevenueChart = Enumerable.Range(0, 14)
            .Select(i => today.AddDays(i - 13))
            .Select(day => new ChartPoint(
                day,
                orders
                    .Where(o => o.Status != OrderStatus.Cancelled && o.PlacedAt >= day && o.PlacedAt < day.AddDays(1))
                    .Sum(o => o.Total)))
            .ToList();
        MaxDailyRevenue = RevenueChart.Count == 0 ? 0 : RevenueChart.Max(p => p.Revenue);
    }
}
