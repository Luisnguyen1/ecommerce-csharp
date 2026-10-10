using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Models;

namespace ECommerce.Core.Services;

public class WishlistService(IRepository<WishlistItem> items, ICatalogService catalog) : IWishlistService
{
    public async Task<bool> ToggleAsync(string userId, string productId, CancellationToken ct = default)
    {
        var all = await items.GetAllAsync(ct);
        var existing = all.FirstOrDefault(w => w.UserId == userId && w.ProductId == productId);
        if (existing is not null)
        {
            await items.RemoveAsync(existing.Id, ct);
            return false;
        }
        await items.AddAsync(new WishlistItem { UserId = userId, ProductId = productId }, ct);
        return true;
    }

    public async Task<bool> ContainsAsync(string userId, string productId, CancellationToken ct = default) =>
        (await items.GetAllAsync(ct)).Any(w => w.UserId == userId && w.ProductId == productId);

    public async Task<HashSet<string>> GetProductIdsAsync(string userId, CancellationToken ct = default) =>
        (await items.GetAllAsync(ct))
            .Where(w => w.UserId == userId)
            .Select(w => w.ProductId)
            .ToHashSet();

    public async Task<IReadOnlyList<ProductCardView>> GetWishlistAsync(string userId, CancellationToken ct = default)
    {
        var ids = await GetProductIdsAsync(userId, ct);
        if (ids.Count == 0) return [];
        var products = await catalog.GetProductsByIdsAsync(ids, ct);
        return await catalog.ToCardViewsAsync(products.Where(p => p.IsActive).ToList(), ct);
    }

    public async Task<int> GetCountAsync(string userId, CancellationToken ct = default) =>
        (await items.GetAllAsync(ct)).Count(w => w.UserId == userId);
}
