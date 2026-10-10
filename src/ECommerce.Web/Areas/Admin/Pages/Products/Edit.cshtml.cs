using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Areas.Admin.Pages.Products;

public class EditModel : PageModel
{
    private readonly ICatalogService _catalog;
    private readonly IRepository<Product> _products;
    private readonly IWebHostEnvironment _environment;

    public EditModel(ICatalogService catalog, IRepository<Product> products, IWebHostEnvironment environment)
    {
        _catalog = catalog;
        _products = products;
        _environment = environment;
    }

    [BindProperty]
    public ProductInputModel Input { get; set; } = new() { IsActive = true };

    public IReadOnlyList<Category> Categories { get; private set; } = [];
    public IReadOnlyList<string> Images { get; private set; } = [];
    public string ProductName { get; private set; } = "";

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var product = await _products.GetByIdAsync(id);
        if (product is null) return NotFound();

        Categories = await _catalog.GetCategoriesAsync(includeInactive: true);
        ProductName = product.Name;
        Images = product.Images;

        var standard = IsStandardVariant(product);
        Input = new ProductInputModel
        {
            Name = product.Name,
            Slug = product.Slug,
            Brand = product.Brand,
            CategoryId = product.CategoryId,
            Price = product.Price,
            CompareAtPrice = product.CompareAtPrice,
            Badge = product.Badge,
            IsFeatured = product.IsFeatured,
            IsActive = product.IsActive,
            Description = product.Description,
            BaseStock = standard ? product.Variants[0].Stock : 0,
            Variants = standard
                ? new List<VariantInput>()
                : product.Variants.Select(v => new VariantInput
                {
                    Id = v.Id,
                    Name = v.Name,
                    Color = v.Color,
                    Size = v.Size,
                    Sku = v.Sku,
                    PriceOverride = v.PriceOverride,
                    Stock = v.Stock
                }).ToList()
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        Categories = await _catalog.GetCategoriesAsync(includeInactive: true);
        Input.Variants ??= [];
        Input.NewImages ??= [];
        Input.RemoveImages ??= [];

        var product = await _products.GetByIdAsync(id);
        if (product is null) return NotFound();
        ProductName = product.Name;
        Images = product.Images;

        var slug = (Input.Slug ?? "").Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(Input.Name))
            return Fail("Product name is required.");
        if (slug.Length == 0)
            return Fail("Slug is required.");
        if (string.IsNullOrEmpty(Input.CategoryId))
            return Fail("Pick a category.");
        if (Input.Price <= 0)
            return Fail("Price must be greater than zero.");

        var all = await _products.GetAllAsync();
        if (all.Any(p => p.Id != product.Id && p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase)))
            return Fail($"The slug \u201C{slug}\u201D is already used by another product.");

        product.Name = Input.Name.Trim();
        product.Slug = slug;
        product.Brand = Input.Brand?.Trim() ?? "";
        product.Description = Input.Description ?? "";
        product.CategoryId = Input.CategoryId;
        product.Price = Input.Price;
        product.CompareAtPrice = Input.CompareAtPrice;
        product.Badge = ProductForm.NullIfWhiteSpace(Input.Badge);
        product.IsFeatured = Input.IsFeatured;
        product.IsActive = Input.IsActive;
        product.Variants = ProductForm.BuildVariants(Input);

        var (saved, skipped) = await ProductForm.SaveUploadsAsync(Input.NewImages, _environment);
        product.Images = product.Images
            .Where(url => !Input.RemoveImages.Contains(url))
            .Concat(saved)
            .ToList();

        await _products.UpdateAsync(product);

        Toast(skipped > 0
            ? $"Product updated — skipped {skipped} invalid image file(s)."
            : "Product updated.");
        return RedirectToPage("/Products/Index", new { area = "Admin" });
    }

    /// <summary>Detects the single default variant saved when no variant rows are used, so it edits as "base stock".</summary>
    private static bool IsStandardVariant(Product product) =>
        product.Variants.Count == 1
        && product.Variants[0].Name == "Standard"
        && string.IsNullOrEmpty(product.Variants[0].Sku)
        && product.Variants[0].Color is null
        && product.Variants[0].Size is null
        && product.Variants[0].PriceOverride is null;

    private IActionResult Fail(string message)
    {
        Toast(message, isError: true);
        return Page();
    }

    private void Toast(string message, bool isError = false)
    {
        TempData["Toast.Message"] = message;
        TempData["Toast.Icon"] = isError ? "alert-circle" : "check-circle";
        TempData["Toast.Type"] = isError ? "error" : "success";
    }
}
