using ECommerce.Core.Entities;

namespace ECommerce.Core.Models;

public enum ProductSort
{
    Newest,
    PriceAsc,
    PriceDesc,
    Name,
    Rating
}

public class ProductQuery
{
    public string? SearchTerm { get; set; }
    public string? CategorySlug { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public ProductSort Sort { get; set; } = ProductSort.Newest;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public bool FeaturedOnly { get; set; }
    public bool IncludeInactive { get; set; } // admin uses this
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

/// <summary>Card-shape view model used by every product grid (home, catalog, wishlist, admin pickers).</summary>
public class ProductCardView
{
    public required string Id { get; init; }
    public required string Slug { get; init; }
    public required string Name { get; init; }
    public required string Brand { get; init; }
    public required string CategoryName { get; init; }
    public required string ImageUrl { get; init; }
    public required decimal FromPrice { get; init; }
    public decimal? CompareAtPrice { get; init; }
    public string? Badge { get; init; }
    public bool IsFeatured { get; init; }
    public int Stock { get; init; }
    public double RatingAverage { get; init; }
    public int RatingCount { get; init; }
    public bool IsActive { get; init; }
    public bool HasVariants { get; init; }
    public string DefaultVariantId { get; init; } = "";
}

public class RatingSummary
{
    public double Average { get; init; }
    public int Count { get; init; }
    public static readonly RatingSummary Empty = new();
}
