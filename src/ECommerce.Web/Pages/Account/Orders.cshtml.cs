using System.Security.Claims;
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages.Account;

[Authorize]
public class OrdersModel(IOrderService orderService) : PageModel
{
    public IReadOnlyList<Order> Orders { get; set; } = [];

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Orders = await orderService.GetOrdersForUserAsync(userId);
    }
}
