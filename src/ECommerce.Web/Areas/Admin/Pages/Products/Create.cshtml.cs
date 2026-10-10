using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Areas.Admin.Pages.Products;

/// <summary>Form shape shared by the product Create and Edit pages.</summary>
public class ProductInputModel
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Brand { get; set; } = "";
    public string CategoryId { get; set; } = "";
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public string? Badge { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public string Description { get; set; } = "";
    /// <summary>Used only when no variant rows are submitted — becomes the stock of one default variant.</summary>
    public int BaseStock { get; set; }
    public List<VariantInput> Variants { get; set; } = [];
    public List<IFormFile> NewImages { get; set; } = [];
    public string[] RemoveImages { get; set; } = [];
}

public class VariantInput
{
    public string? Id { get; set; }
    public string Name { get; set; } = "";
    public string? Color { get; set; }
    public string? Size { get; set; }
    public string Sku { get; set; } = "";
    public decimal? PriceOverride { get; set; }
    public int Stock { get; set; }

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Name)
        && string.IsNullOrWhiteSpace(Color)
        && string.IsNullOrWhiteSpace(Size)
        && string.IsNullOrWhiteSpace(Sku)
        && PriceOverride is null
        && Stock == 0;
}

/// <summary>Shared mapping/persistence helpers for the product form (also used by the Edit page).</summary>
internal static class ProductForm
{
    public const long MaxImageBytes = 2 * 1024 * 1024;
    public static readonly string[] AllowedImageExtensions = [".png", ".jpg", ".jpeg", ".webp", ".gif", ".svg"];

    public static List<ProductVariant> BuildVariants(ProductInputModel input)
    {
        var rows = input.Variants.Where(v => v is not null && !v.IsEmpty).ToList();
        if (rows.Count == 0)
        {
            // Single standard product: the required stock lives in one default variant.
            return [new ProductVariant { Name = "Standard", Stock = input.BaseStock, IsActive = true }];
        }

        return rows.Select(v => new ProductVariant
        {
            Id = string.IsNullOrWhiteSpace(v.Id) ? Guid.NewGuid().ToString("N") : v.Id,
            Name = v.Name.Trim(),
            Color = NullIfWhiteSpace(v.Color),
            Size = NullIfWhiteSpace(v.Size),
            Sku = v.Sku?.Trim() ?? "",
            PriceOverride = v.PriceOverride,
            Stock = v.Stock,
            IsActive = true
        }).ToList();
    }

    /// <summary>Saves valid uploads to wwwroot/uploads/products. Files over 2 MB or with an unexpected extension are skipped and counted.</summary>
    public static async Task<(List<string> Saved, int Skipped)> SaveUploadsAsync(
        IEnumerable<IFormFile> files, IWebHostEnvironment environment)
    {
        var saved = new List<string>();
        var skipped = 0;
        var list = files as IList<IFormFile> ?? files.ToList();
        if (list.Count == 0) return (saved, skipped);

        var directory = Path.Combine(
            environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"),
            "uploads", "products");
        Directory.CreateDirectory(directory);

        foreach (var file in list)
        {
            if (file is null || file.Length == 0 || file.Length > MaxImageBytes)
            {
                skipped++;
                continue;
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(extension))
            {
                skipped++;
                continue;
            }

            var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}".ToLowerInvariant();
            await using var stream = File.Create(Path.Combine(directory, fileName));
            await file.CopyToAsync(stream);
            saved.Add($"/uploads/products/{fileName}");
        }

        return (saved, skipped);
    }

    public static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public class CreateModel : PageModel
{
    private readonly ICatalogService _catalog;
    private readonly IRepository<Product> _products;
    private readonly IWebHostEnvironment _environment;

    public CreateModel(ICatalogService catalog, IRepository<Product> products, IWebHostEnvironment environment)
    {
        _catalog = catalog;
        _products = products;
        _environment = environment;
    }

    [BindProperty]
    public ProductInputModel Input { get; set; } = new() { IsActive = true };

    public IReadOnlyList<Category> Categories { get; private set; } = [];

    public async Task OnGetAsync() =>
        Categories = await _catalog.GetCategoriesAsync(includeInactive: true);

    public async Task<IActionResult> OnPostAsync()
    {
        Categories = await _catalog.GetCategoriesAsync(includeInactive: true);
        Input.Variants ??= [];
        Input.NewImages ??= [];
        Input.RemoveImages ??= [];

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
        if (all.Any(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase)))
            return Fail($"The slug \u201C{slug}\u201D is already used by another product.");

        var (savedImages, skipped) = await ProductForm.SaveUploadsAsync(Input.NewImages, _environment);

        var product = new Product
        {
            Name = Input.Name.Trim(),
            Slug = slug,
            Brand = Input.Brand?.Trim() ?? "",
            Description = Input.Description ?? "",
            CategoryId = Input.CategoryId,
            Price = Input.Price,
            CompareAtPrice = Input.CompareAtPrice,
            Badge = ProductForm.NullIfWhiteSpace(Input.Badge),
            IsFeatured = Input.IsFeatured,
            IsActive = Input.IsActive,
            Images = savedImages
        };
        product.Variants = ProductForm.BuildVariants(Input);

        await _products.AddAsync(product);

        Toast(skipped > 0
            ? $"Product created — skipped {skipped} invalid image file(s)."
            : "Product created.");
        return RedirectToPage("/Products/Index", new { area = "Admin" });
    }

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
