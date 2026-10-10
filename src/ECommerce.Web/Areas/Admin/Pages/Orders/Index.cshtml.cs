using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Areas.Admin.Pages.Orders;

public class IndexModel : PageModel
{
    private readonly IOrderService _orderService;

    public IndexModel(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public OrderStatus? Status { get; private set; }
    public IReadOnlyList<Order> Orders { get; private set; } = [];
    public IReadOnlyList<(string Label, int Count)> StatusCounts { get; private set; } = [];
    public int TotalCount => StatusCounts.Sum(c => c.Count);

    public int CountFor(OrderStatus status) =>
        StatusCounts.FirstOrDefault(c => c.Label == status.ToString()).Count;

    public async Task OnGetAsync(string? status)
    {
        StatusCounts = await _orderService.GetStatusCountsAsync();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var parsed))
            Status = parsed;

        Orders = await _orderService.GetAllAsync(Status);
    }
}
