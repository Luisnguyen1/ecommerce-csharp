using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Infrastructure;

public class CartBadgeViewComponent(ICartService carts) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var (ownerId, _) = ViewContext.HttpContext.Resolve();
        return View(await carts.GetItemCountAsync(ownerId));
    }
}

public class WishlistBadgeViewComponent(IWishlistService wishlist) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (UserClaimsPrincipal.Identity?.IsAuthenticated != true) return View(0);
        var userId = UserClaimsPrincipal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        return View(await wishlist.GetCountAsync(userId));
    }
}

public class CategoriesNavViewComponent(ICatalogService catalog) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var categories = await catalog.GetCategoriesAsync();
        return View(categories);
    }
}
