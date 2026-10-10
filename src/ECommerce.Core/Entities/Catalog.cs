namespace ECommerce.Core.Entities;

public class Category : Entity
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? Icon { get; set; } // emoji used in nav/cards
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Product : Entity
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Sku { get; set; } = "";
    public string Brand { get; set; } = "";
    public string Description { get; set; } = "";
    public string CategoryId { get; set; } = "";
    public decimal Price { get; set; }
    /// <summary>Original price to show as a strikethrough "compare at" price for sale items.</summary>
    public decimal? CompareAtPrice { get; set; }
    public List<string> Images { get; set; } = [];
    public List<ProductVariant> Variants { get; set; } = [];
    public string? Badge { get; set; } // e.g. "New", "Sale", "Hot"
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ProductVariant
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";       // e.g. "Black / L"
    public string? Color { get; set; }
    public string? Size { get; set; }
    public string Sku { get; set; } = "";
    public decimal? PriceOverride { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;

    public decimal EffectivePrice(decimal basePrice) => PriceOverride ?? basePrice;
}

public static class ProductExtensions
{
    /// <summary>Lowest effective price across active variants (or base price when there are none).</summary>
    public static decimal FromPrice(this Product product)
    {
        var active = product.Variants.Where(v => v.IsActive).ToList();
        return active.Count == 0 ? product.Price : active.Min(v => v.EffectivePrice(product.Price));
    }

    public static int TotalStock(this Product product) => product.Variants.Sum(v => v.IsActive ? v.Stock : 0);
}
