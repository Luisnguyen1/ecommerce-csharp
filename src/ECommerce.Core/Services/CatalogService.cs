using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Models;

namespace ECommerce.Core.Services;

public class CatalogService(
    IRepository<Product> products,
    IRepository<Category> categories,
    IReviewService reviews) : ICatalogService
{
    public async Task<PagedResult<ProductCardView>> SearchAsync(ProductQuery query, CancellationToken ct = default)
    {
        var all = (await products.GetAllAsync(ct)).Where(p => query.IncludeInactive || p.IsActive);

        if (query.FeaturedOnly)
            all = all.Where(p => p.IsFeatured);

        if (!string.IsNullOrWhiteSpace(query.CategorySlug))
        {
            var category = await GetCategoryBySlugAsync(query.CategorySlug, ct);
            if (category is null)
                return new PagedResult<ProductCardView> { Items = [], TotalCount = 0, Page = 1, PageSize = query.PageSize };
            var categoryId = category.Id;
            all = all.Where(p => p.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim();
            all = all.Where(p =>
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Brand.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Sku.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (query.MinPrice is { } min)
            all = all.Where(p => p.FromPrice() >= min);
        if (query.MaxPrice is { } max)
            all = all.Where(p => p.FromPrice() <= max);

        var summaries = await reviews.GetSummariesAsync(ct);

        all = query.Sort switch
        {
            ProductSort.PriceAsc => all.OrderBy(p => p.FromPrice()),
            ProductSort.PriceDesc => all.OrderByDescending(p => p.FromPrice()),
            ProductSort.Name => all.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase),
            ProductSort.Rating => all.OrderByDescending(p => summaries.GetValueOrDefault(p.Id).Average),
            _ => all.OrderByDescending(p => p.CreatedAt)
        };

        var page = Math.Max(1, query.Page);
        var total = all.Count();
        var paged = all.Skip((page - 1) * query.PageSize).Take(query.PageSize).ToList();

        return new PagedResult<ProductCardView>
        {
            Items = await ToCardViewsAsync(paged, ct),
            TotalCount = total,
            Page = page,
            PageSize = query.PageSize
        };
    }

    public async Task<Product?> GetBySlugAsync(string slug, bool includeInactive = false, CancellationToken ct = default)
    {
        var product = (await products.GetAllAsync(ct))
            .FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
        if (product is null || (!includeInactive && !product.IsActive))
            return null;
        return product;
    }

    public Task<Product?> GetByIdAsync(string id, CancellationToken ct = default) => products.GetByIdAsync(id, ct);

    public async Task<IReadOnlyList<Product>> GetProductsByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default)
    {
        var idSet = ids.ToHashSet();
        return (await products.GetAllAsync(ct)).Where(p => idSet.Contains(p.Id)).ToList();
    }

    public async Task<IReadOnlyList<ProductCardView>> GetFeaturedAsync(int count = 8, CancellationToken ct = default)
    {
        var featured = (await products.GetAllAsync(ct))
            .Where(p => p.IsActive && p.IsFeatured)
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToList();
        return await ToCardViewsAsync(featured, ct);
    }

    public async Task<IReadOnlyList<ProductCardView>> GetNewArrivalsAsync(int count = 8, CancellationToken ct = default)
    {
        var newest = (await products.GetAllAsync(ct))
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToList();
        return await ToCardViewsAsync(newest, ct);
    }

    public async Task<IReadOnlyList<ProductCardView>> GetRelatedAsync(string productId, int count = 4, CancellationToken ct = default)
    {
        var product = await products.GetByIdAsync(productId, ct);
        if (product is null) return [];

        var related = (await products.GetAllAsync(ct))
            .Where(p => p.IsActive && p.Id != product.Id && p.CategoryId == product.CategoryId)
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.CreatedAt)
            .Take(count)
            .ToList();

        // Top up from other categories when the category is thin.
        if (related.Count < count)
        {
            var relatedIds = related.Select(p => p.Id).ToHashSet();
            related.AddRange((await products.GetAllAsync(ct))
                .Where(p => p.IsActive && p.Id != product.Id && p.CategoryId != product.CategoryId && !relatedIds.Contains(p.Id))
                .OrderByDescending(p => p.CreatedAt)
                .Take(count - related.Count));
        }
        return await ToCardViewsAsync(related, ct);
    }

    public async Task<IReadOnlyList<ProductCardView>> ToCardViewsAsync(IReadOnlyList<Product> list, CancellationToken ct = default)
    {
        var categoryNames = (await categories.GetAllAsync(ct)).ToDictionary(c => c.Id, c => c.Name);
        var summaries = await reviews.GetSummariesAsync(ct);

        return list.Select(p =>
        {
            var summary = summaries.GetValueOrDefault(p.Id);
            var activeVariants = p.Variants.Where(v => v.IsActive).ToList();
            return new ProductCardView
            {
                Id = p.Id,
                Slug = p.Slug,
                Name = p.Name,
                Brand = p.Brand,
                CategoryName = categoryNames.GetValueOrDefault(p.CategoryId, ""),
                ImageUrl = p.Images.FirstOrDefault() ?? "/images/catalog/placeholder.svg",
                FromPrice = p.FromPrice(),
                CompareAtPrice = p.CompareAtPrice,
                Badge = p.Badge,
                IsFeatured = p.IsFeatured,
                Stock = p.TotalStock(),
                RatingAverage = summary?.Average ?? 0,
                RatingCount = summary?.Count ?? 0,
                IsActive = p.IsActive,
                HasVariants = activeVariants.Count > 1,
                DefaultVariantId = activeVariants.Count == 1 ? activeVariants[0].Id : ""
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        return (await categories.GetAllAsync(ct))
            .Where(c => includeInactive || c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToList();
    }

    public async Task<Category?> GetCategoryBySlugAsync(string slug, CancellationToken ct = default) =>
        (await categories.GetAllAsync(ct))
            .FirstOrDefault(c => c.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
}
