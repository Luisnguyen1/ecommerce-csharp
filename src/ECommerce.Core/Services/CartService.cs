using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Models;

namespace ECommerce.Core.Services;

public class CartService(
    IRepository<Cart> carts,
    IRepository<Product> products,
    ICouponService coupons) : ICartService
{
    public async Task<CartView> GetCartViewAsync(string ownerId, CancellationToken ct = default)
    {
        var cart = await FindAsync(ownerId, ct);
        if (cart is null || cart.Items.Count == 0)
            return new CartView { Lines = [], ItemCount = 0 };

        var productIds = cart.Items.Select(i => i.ProductId).ToHashSet();
        var catalog = (await products.GetAllAsync(ct))
            .Where(p => productIds.Contains(p.Id))
            .ToDictionary(p => p.Id);

        var lines = new List<CartLineView>();
        foreach (var item in cart.Items)
        {
            if (!catalog.TryGetValue(item.ProductId, out var product)) continue;
            var variant = product.Variants.FirstOrDefault(v => v.Id == item.VariantId);
            if (variant is not null && !variant.IsActive) continue;

            var unitPrice = variant?.EffectivePrice(product.Price) ?? product.Price;
            // Self-heal the price snapshot so long-lived carts reflect current prices.
            if (item.UnitPrice != unitPrice) item.UnitPrice = unitPrice;

            lines.Add(new CartLineView
            {
                ProductId = item.ProductId,
                VariantId = item.VariantId,
                ProductName = product.Name,
                ProductSlug = product.Slug,
                VariantName = variant?.Name ?? "Standard",
                ImageUrl = product.Images.FirstOrDefault() ?? "/images/catalog/placeholder.svg",
                UnitPrice = unitPrice,
                Quantity = item.Quantity,
                StockAvailable = variant?.Stock ?? product.TotalStock()
            });
        }

        if (lines.Count != cart.Items.Count) // some referenced products vanished
        {
            await carts.UpdateAsync(cart, ct);
        }

        return new CartView
        {
            Lines = lines,
            AppliedCouponCode = cart.AppliedCouponCode,
            ItemCount = lines.Sum(l => l.Quantity)
        };
    }

    public async Task<int> GetItemCountAsync(string ownerId, CancellationToken ct = default)
    {
        var cart = await FindAsync(ownerId, ct);
        return cart?.Items.Sum(i => i.Quantity) ?? 0;
    }

    public async Task<Result> AddItemAsync(string ownerId, bool isAnonymous, string productId, string? variantId, int quantity, CancellationToken ct = default)
    {
        if (quantity < 1) return Result.Fail("Quantity must be at least 1.");

        var product = await products.GetByIdAsync(productId, ct);
        if (product is null || !product.IsActive) return Result.Fail("This product is no longer available.");

        var variant = ResolveVariant(product, variantId);
        if (variant is null) return Result.Fail("Please choose an available option.");

        var existingCart = await FindAsync(ownerId, ct);
        var cart = existingCart ?? new Cart { OwnerId = ownerId, IsAnonymous = isAnonymous };
        var existing = cart.Items.FirstOrDefault(i => i.ProductId == productId && i.VariantId == variant.Id);
        var desired = (existing?.Quantity ?? 0) + quantity;

        if (desired > variant.Stock)
            return Result.Fail($"Only {variant.Stock} left in stock.");

        if (existing is null)
        {
            cart.Items.Add(new CartItem
            {
                ProductId = productId,
                VariantId = variant.Id,
                Quantity = quantity,
                UnitPrice = variant.EffectivePrice(product.Price)
            });
        }
        else
        {
            existing.Quantity = desired;
            existing.UnitPrice = variant.EffectivePrice(product.Price);
        }

        cart.UpdatedAt = DateTime.UtcNow;
        if (existingCart is null) await carts.AddAsync(cart, ct);
        else await carts.UpdateAsync(cart, ct);
        return Result.Ok();
    }

    public async Task<Result> SetQuantityAsync(string ownerId, string productId, string variantId, int quantity, CancellationToken ct = default)
    {
        var cart = await FindAsync(ownerId, ct);
        if (cart is null) return Result.Fail("Cart not found.");

        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId && i.VariantId == variantId);
        if (item is null) return Result.Fail("Item not found.");

        if (quantity <= 0)
        {
            cart.Items.Remove(item);
        }
        else
        {
            var product = await products.GetByIdAsync(productId, ct);
            var variant = product?.Variants.FirstOrDefault(v => v.Id == variantId);
            if (variant is not null && quantity > variant.Stock)
                return Result.Fail($"Only {variant.Stock} left in stock.");
            item.Quantity = quantity;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await carts.UpdateAsync(cart, ct);
        return Result.Ok();
    }

    public async Task<Result> RemoveItemAsync(string ownerId, string productId, string variantId, CancellationToken ct = default)
    {
        var cart = await FindAsync(ownerId, ct);
        if (cart is null) return Result.Fail("Cart not found.");

        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId && i.VariantId == variantId);
        if (item is null) return Result.Fail("Item not found.");

        cart.Items.Remove(item);
        cart.UpdatedAt = DateTime.UtcNow;
        await carts.UpdateAsync(cart, ct);
        return Result.Ok();
    }

    public async Task ClearAsync(string ownerId, CancellationToken ct = default)
    {
        var cart = await FindAsync(ownerId, ct);
        if (cart is null) return;
        cart.Items.Clear();
        cart.AppliedCouponCode = null;
        cart.UpdatedAt = DateTime.UtcNow;
        await carts.UpdateAsync(cart, ct);
    }

    public async Task MergeAsync(string anonymousOwnerId, string userId, CancellationToken ct = default)
    {
        var anon = await FindAsync(anonymousOwnerId, ct);
        if (anon is null || anon.Items.Count == 0) return;

        var existingUserCart = await FindAsync(userId, ct);
        var userCart = existingUserCart ?? new Cart { OwnerId = userId, IsAnonymous = false };

        foreach (var item in anon.Items)
        {
            var existing = userCart.Items.FirstOrDefault(i => i.ProductId == item.ProductId && i.VariantId == item.VariantId);
            if (existing is null)
                userCart.Items.Add(item);
            else
                existing.Quantity += item.Quantity;
        }

        userCart.UpdatedAt = DateTime.UtcNow;
        if (existingUserCart is null) await carts.AddAsync(userCart, ct);
        else await carts.UpdateAsync(userCart, ct);
        await carts.RemoveAsync(anon.Id, ct);
    }

    public async Task<Result> ApplyCouponAsync(string ownerId, string code, CancellationToken ct = default)
    {
        var cart = await FindAsync(ownerId, ct);
        if (cart is null || cart.Items.Count == 0) return Result.Fail("Your cart is empty.");

        var subtotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);
        var validation = await coupons.ValidateAsync(code, subtotal, ct);
        if (!validation.IsValid) return Result.Fail(validation.Error ?? "This coupon is not valid.");

        cart.AppliedCouponCode = validation.Coupon!.Code;
        await carts.UpdateAsync(cart, ct);
        return Result.Ok();
    }

    public async Task RemoveCouponAsync(string ownerId, CancellationToken ct = default)
    {
        var cart = await FindAsync(ownerId, ct);
        if (cart is null) return;
        cart.AppliedCouponCode = null;
        await carts.UpdateAsync(cart, ct);
    }

    public async Task<CartTotals> GetTotalsAsync(string ownerId, string? shippingMethodId = null, CancellationToken ct = default)
    {
        var cart = await FindAsync(ownerId, ct);
        var subtotal = cart?.Items.Sum(i => i.UnitPrice * i.Quantity) ?? 0;

        string? couponCode = null;
        decimal discount = 0;
        string? couponError = null;

        if (cart?.AppliedCouponCode is { } applied)
        {
            var validation = await coupons.ValidateAsync(applied, subtotal, ct);
            if (validation.IsValid)
            {
                couponCode = validation.Coupon!.Code;
                discount = validation.DiscountAmount;
            }
            else
            {
                couponError = validation.Error;
            }
        }

        var shipping = Shipping.Resolve(shippingMethodId, subtotal);

        return new CartTotals
        {
            Subtotal = subtotal,
            Discount = discount,
            CouponCode = couponCode,
            CouponError = couponError,
            ShippingFee = shipping?.Fee ?? 0,
            ShippingMethodId = shipping?.Id
        };
    }

    private async Task<Cart?> FindAsync(string ownerId, CancellationToken ct) =>
        (await carts.GetAllAsync(ct)).FirstOrDefault(c => c.OwnerId == ownerId);

    private static ProductVariant? ResolveVariant(Product product, string? variantId)
    {
        var active = product.Variants.Where(v => v.IsActive).ToList();
        if (active.Count == 0) return new ProductVariant { Id = "", Name = "Standard", Stock = int.MaxValue };
        return active.FirstOrDefault(v => v.Id == variantId) ?? active.FirstOrDefault();
    }
}
