using System.Security.Claims;
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages.Catalog;

public class IndexModel(ICatalogService catalog, IWishlistService wishlist) : PageModel
{
    private static readonly Dictionary<string, ProductSort> SortMap = new()
    {
        ["newest"] = ProductSort.Newest,
        ["price-asc"] = ProductSort.PriceAsc,
        ["price-desc"] = ProductSort.PriceDesc,
        ["name"] = ProductSort.Name,
        ["rating"] = ProductSort.Rating
    };

    [BindProperty(SupportsGet = true)] public string? Q { get; set; }
    [BindProperty(SupportsGet = true)] public string? Category { get; set; }
    [BindProperty(SupportsGet = true)] public decimal? Min { get; set; }
    [BindProperty(SupportsGet = true)] public decimal? Max { get; set; }
    [BindProperty(SupportsGet = true)] public string? Sort { get; set; }
    [BindProperty(SupportsGet = true, Name = "page")] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public bool Featured { get; set; }

    public IReadOnlyList<Category> Categories { get; private set; } = [];
    public Category? CurrentCategory { get; private set; }
    public PagedResult<ProductCardView> Result { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken ct = default)
    {
        Categories = await catalog.GetCategoriesAsync(ct: ct);
        CurrentCategory = string.IsNullOrWhiteSpace(Category)
            ? null
            : await catalog.GetCategoryBySlugAsync(Category, ct);

        var sort = Sort is not null && SortMap.TryGetValue(Sort, out var parsed) ? parsed : ProductSort.Newest;

        Result = await catalog.SearchAsync(new ProductQuery
        {
            SearchTerm = string.IsNullOrWhiteSpace(Q) ? null : Q.Trim(),
            CategorySlug = CurrentCategory?.Slug,
            MinPrice = Min,
            MaxPrice = Max,
            Sort = sort,
            Page = Math.Max(1, PageNumber),
            PageSize = 12,
            FeaturedOnly = Featured
        }, ct);

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is not null)
                ViewData["WishlistIds"] = await wishlist.GetProductIdsAsync(userId, ct);
        }

        ViewData["Title"] = !string.IsNullOrWhiteSpace(Q) ? $"Search \u201c{Q.Trim()}\u201d"
            : CurrentCategory?.Name ?? (Featured ? "Featured products" : "Shop");
    }
}
