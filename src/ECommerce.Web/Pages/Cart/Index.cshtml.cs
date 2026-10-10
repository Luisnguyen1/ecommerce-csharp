using ECommerce.Core.Interfaces;
using ECommerce.Core.Models;
using ECommerce.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages.Cart;

public class IndexModel(ICartService cartService) : PageModel
{
    public CartView Cart { get; set; } = new() { Lines = [] };
    public CartTotals Totals { get; set; } = new();

    public async Task OnGetAsync()
    {
        var (ownerId, _) = HttpContext.Resolve();
        Cart = await cartService.GetCartViewAsync(ownerId);
        Totals = await cartService.GetTotalsAsync(ownerId);
    }

    // Used by _ProductCard across the storefront.
    public async Task<IActionResult> OnPostAddAsync(string productId, string? variantId, int quantity, string? returnUrl)
    {
        var (ownerId, isAnonymous) = HttpContext.Resolve(createCookie: true);
        if (quantity < 1) quantity = 1;

        var result = await cartService.AddItemAsync(ownerId, isAnonymous, productId, variantId, quantity);
        if (result.Success)
            Success("Added to cart", "shopping-cart");
        else
            Error(result.Error ?? "Could not add this item.");

        return RedirectBack(returnUrl);
    }

    public async Task<IActionResult> OnPostSetQuantityAsync(string productId, string variantId, int quantity)
    {
        var (ownerId, _) = HttpContext.Resolve(createCookie: true);
        var result = await cartService.SetQuantityAsync(ownerId, productId, variantId, quantity);
        if (!result.Success)
            Error(result.Error ?? "Could not update the quantity.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveAsync(string productId, string variantId)
    {
        var (ownerId, _) = HttpContext.Resolve(createCookie: true);
        var result = await cartService.RemoveItemAsync(ownerId, productId, variantId);
        if (!result.Success)
            Error(result.Error ?? "Could not remove this item.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostApplyCouponAsync(string? code)
    {
        var (ownerId, _) = HttpContext.Resolve(createCookie: true);
        var normalized = code?.Trim().ToUpperInvariant();

        var result = await cartService.ApplyCouponAsync(ownerId, normalized);
        if (!result.Success)
        {
            Error(result.Error ?? "This coupon is not valid.");
            return RedirectToPage();
        }

        var totals = await cartService.GetTotalsAsync(ownerId);
        Success(totals.Discount > 0
            ? $"Coupon {totals.CouponCode ?? normalized} applied — you saved {totals.Discount.Money()}."
            : "Coupon applied.", "tag");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveCouponAsync()
    {
        var (ownerId, _) = HttpContext.Resolve(createCookie: true);
        await cartService.RemoveCouponAsync(ownerId);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostClearAsync()
    {
        var (ownerId, _) = HttpContext.Resolve(createCookie: true);
        await cartService.ClearAsync(ownerId);
        Success("Cart cleared.");
        return RedirectToPage();
    }

    private void Success(string message, string icon = "check-circle") => Toast(message, icon, "success");
    private void Error(string message) => Toast(message, "alert-circle", "error");

    private void Toast(string message, string icon, string type)
    {
        TempData["Toast.Message"] = message;
        TempData["Toast.Icon"] = icon;
        TempData["Toast.Type"] = type;
    }

    private IActionResult RedirectBack(string? url) =>
        Url.IsLocalUrl(url) ? Redirect(url) : RedirectToPage();
}
