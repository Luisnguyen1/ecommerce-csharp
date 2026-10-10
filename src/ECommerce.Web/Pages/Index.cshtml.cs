using System.Security.Claims;
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages;

public class IndexModel(ICatalogService catalog, IWishlistService wishlist) : PageModel
{
    public IReadOnlyList<Category> Categories { get; private set; } = [];
    public IReadOnlyList<ProductCardView> Featured { get; private set; } = [];
    public IReadOnlyList<ProductCardView> NewArrivals { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct = default)
    {
        Categories = await catalog.GetCategoriesAsync(ct: ct);
        Featured = await catalog.GetFeaturedAsync(8, ct);
        NewArrivals = await catalog.GetNewArrivalsAsync(8, ct);

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is not null)
                ViewData["WishlistIds"] = await wishlist.GetProductIdsAsync(userId, ct);
        }

        ViewData["Title"] = "Home";
    }
}
