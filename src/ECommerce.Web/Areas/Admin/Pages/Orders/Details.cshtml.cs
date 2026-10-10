using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Areas.Admin.Pages.Orders;

public class DetailsModel : PageModel
{
    private readonly IOrderService _orderService;

    public DetailsModel(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public Order? Order { get; private set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        Order = await _orderService.GetByIdAsync(id);
        return Order is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAdvanceAsync(string id, OrderStatus to)
    {
        var result = await _orderService.AdvanceStatusAsync(id, to);

        TempData["Toast.Message"] = result.Success ? $"Order marked as {to.ToString().ToLowerInvariant()}." : result.Error;
        TempData["Toast.Icon"] = result.Success ? "check-circle" : "alert-circle";
        TempData["Toast.Type"] = result.Success ? "success" : "error";

        return RedirectToPage("Details", new { id });
    }
}
