using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Areas.Admin.Pages.Products;

public class IndexModel : PageModel
{
    private readonly ICatalogService _catalog;
    private readonly IRepository<Product> _products;

    public IndexModel(ICatalogService catalog, IRepository<Product> products)
    {
        _catalog = catalog;
        _products = products;
    }

    public PagedResult<ProductCardView> Results { get; private set; } = new();
    public string? Q { get; private set; }

    /// <summary>Full entities behind the card views, for variant-level stock checks.</summary>
    public Dictionary<string, Product> ProductMap { get; private set; } = [];

    public async Task OnGetAsync(string? q)
    {
        Q = q;
        Results = await _catalog.SearchAsync(new ProductQuery
        {
            IncludeInactive = true,
            SearchTerm = q,
            PageSize = 500
        });

        var entities = await _catalog.GetProductsByIdsAsync(Results.Items.Select(p => p.Id));
        ProductMap = entities.ToDictionary(p => p.Id);
    }

    public bool IsLowStock(string productId) =>
        ProductMap.TryGetValue(productId, out var p)
        && p.Variants.Any(v => v.IsActive && v.Stock <= 5);

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        await _products.RemoveAsync(id);
        TempData["Toast.Message"] = "Product deleted.";
        TempData["Toast.Icon"] = "check-circle";
        TempData["Toast.Type"] = "success";
        return RedirectToPage(new { q = Q });
    }
}
