using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Areas.Admin.Pages.Categories;

public class IndexModel : PageModel
{
    private readonly IRepository<Category> _categories;
    private readonly IRepository<Product> _products;

    public IndexModel(IRepository<Category> categories, IRepository<Product> products)
    {
        _categories = categories;
        _products = products;
    }

    public List<Category> Categories { get; private set; } = [];
    public Dictionary<string, int> ProductCounts { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var categories = await _categories.GetAllAsync();
        var products = await _products.GetAllAsync();
        Categories = categories.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
        ProductCounts = products.GroupBy(p => p.CategoryId).ToDictionary(g => g.Key, g => g.Count());
    }

    public int CountFor(Category category) => ProductCounts.GetValueOrDefault(category.Id);

    public async Task<IActionResult> OnPostCreateAsync(string name, string slug, string? icon, string? description, int displayOrder, bool isActive = false)
    {
        var cleanName = (name ?? "").Trim();
        var cleanSlug = (slug ?? "").Trim().ToLowerInvariant();

        if (cleanName.Length == 0 || cleanSlug.Length == 0)
        {
            Toast("Name and slug are required.", isError: true);
            return RedirectToPage();
        }

        var all = await _categories.GetAllAsync();
        if (all.Any(c => c.Slug.Equals(cleanSlug, StringComparison.OrdinalIgnoreCase)))
        {
            Toast($"A category with slug \u201C{cleanSlug}\u201D already exists.", isError: true);
            return RedirectToPage();
        }

        await _categories.AddAsync(new Category
        {
            Name = cleanName,
            Slug = cleanSlug,
            Icon = NullIfWhiteSpace(icon),
            Description = NullIfWhiteSpace(description),
            DisplayOrder = displayOrder,
            IsActive = isActive
        });

        Toast($"Category \u201C{cleanName}\u201D created.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(string id, string name, string slug, string? icon, string? description, int displayOrder, bool isActive = false)
    {
        var category = await _categories.GetByIdAsync(id);
        if (category is null)
        {
            Toast("Category not found.", isError: true);
            return RedirectToPage();
        }

        var cleanName = (name ?? "").Trim();
        var cleanSlug = (slug ?? "").Trim().ToLowerInvariant();
        if (cleanName.Length == 0 || cleanSlug.Length == 0)
        {
            Toast("Name and slug are required.", isError: true);
            return RedirectToPage();
        }

        var all = await _categories.GetAllAsync();
        if (all.Any(c => c.Id != category.Id && c.Slug.Equals(cleanSlug, StringComparison.OrdinalIgnoreCase)))
        {
            Toast($"A category with slug \u201C{cleanSlug}\u201D already exists.", isError: true);
            return RedirectToPage();
        }

        category.Name = cleanName;
        category.Slug = cleanSlug;
        category.Icon = NullIfWhiteSpace(icon);
        category.Description = NullIfWhiteSpace(description);
        category.DisplayOrder = displayOrder;
        category.IsActive = isActive;
        await _categories.UpdateAsync(category);

        Toast($"Category \u201C{cleanName}\u201D updated.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        await _categories.RemoveAsync(id);
        Toast("Category deleted.");
        return RedirectToPage();
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void Toast(string message, bool isError = false)
    {
        TempData["Toast.Message"] = message;
        TempData["Toast.Icon"] = isError ? "alert-circle" : "check-circle";
        TempData["Toast.Type"] = isError ? "error" : "success";
    }
}
