using System.Security.Claims;
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages.Account.Orders;

[Authorize]
public class DetailsModel(IOrderService orderService) : PageModel
{
    public Order Order { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return NotFound();

        var order = await orderService.GetByIdAsync(id);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (order is null || order.UserId != userId)
            return NotFound();

        Order = order;
        return Page();
    }
}
