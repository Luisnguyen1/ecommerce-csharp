using System.Security.Claims;
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace ECommerce.Web.Pages.Product;

public class DetailsModel(ICatalogService catalog, IReviewService reviews, IWishlistService wishlist) : PageModel
{
    public ECommerce.Core.Entities.Product Product { get; private set; } = null!;
    public Category? Category { get; private set; }
    public IReadOnlyList<Review> Reviews { get; private set; } = [];
    public RatingSummary Rating { get; private set; } = RatingSummary.Empty;
    public IReadOnlyList<ProductCardView> Related { get; private set; } = [];
    public bool InWishlist { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        Product = await catalog.GetBySlugAsync(slug, ct: ct);
        if (Product is null)
            return NotFound();

        var categories = await catalog.GetCategoriesAsync(ct: ct);
        Category = categories.FirstOrDefault(c => c.Id == Product.CategoryId);

        Reviews = await reviews.GetForProductAsync(Product.Id, ct);
        Rating = Reviews.Count == 0
            ? RatingSummary.Empty
            : new RatingSummary { Average = Reviews.Average(r => r.Rating), Count = Reviews.Count };

        Related = await catalog.GetRelatedAsync(Product.Id, 4, ct);

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is not null)
            {
                InWishlist = await wishlist.ContainsAsync(userId, Product.Id, ct);
                ViewData["WishlistIds"] = await wishlist.GetProductIdsAsync(userId, ct);
            }
        }

        ViewData["Title"] = Product.Name;
        return Page();
    }

    public async Task<IActionResult> OnPostAddReviewAsync(string slug, int rating, string title, string body, CancellationToken ct = default)
    {
        if (User.Identity?.IsAuthenticated != true || User.Identity.Name is null)
            return Redirect($"/Account/Login?ReturnUrl={Uri.EscapeDataString($"/Product/{slug}")}");

        var product = await catalog.GetBySlugAsync(slug, ct: ct);
        if (product is null)
            return NotFound();

        var result = await reviews.UpsertAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            User.Identity.Name,
            product.Id,
            Math.Clamp(rating, 1, 5),
            title?.Trim() ?? "",
            body?.Trim() ?? "",
            ct);

        TempData["Toast.Message"] = result.Success
            ? "Thanks — your review has been saved."
            : result.Error ?? "Could not save your review.";
        TempData["Toast.Icon"] = result.Success ? "check-circle" : "alert-circle";
        TempData["Toast.Type"] = result.Success ? "success" : "error";

        return RedirectToPage(new { slug });
    }
}
