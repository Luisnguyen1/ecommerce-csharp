using System.Security.Claims;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages.Account;

[Authorize]
public class WishlistModel(IWishlistService wishlistService) : PageModel
{
    public IReadOnlyList<ProductCardView> Products { get; set; } = [];

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Products = await wishlistService.GetWishlistAsync(userId);
        ViewData["WishlistIds"] = await wishlistService.GetProductIdsAsync(userId);
    }

    // Used by _ProductCard across the storefront.
    public async Task<IActionResult> OnPostToggleAsync(string productId, string? returnUrl)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var added = await wishlistService.ToggleAsync(userId, productId);

        TempData["Toast.Message"] = added ? "Saved to wishlist" : "Removed from wishlist";
        TempData["Toast.Icon"] = "heart";
        TempData["Toast.Type"] = "success";

        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToPage();
    }
}
