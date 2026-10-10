using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages.Checkout;

public class SuccessModel(IOrderService orderService) : PageModel
{
    public Order Order { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(string? orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            return RedirectToPage("/Index");

        var order = await orderService.GetByOrderNumberAsync(orderNumber);
        if (order is null)
            return NotFound();

        Order = order;
        return Page();
    }
}
