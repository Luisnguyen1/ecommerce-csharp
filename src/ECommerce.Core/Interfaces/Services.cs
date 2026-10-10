using ECommerce.Core.Entities;
using ECommerce.Core.Models;

namespace ECommerce.Core.Interfaces;

public interface ICatalogService
{
    Task<PagedResult<ProductCardView>> SearchAsync(ProductQuery query, CancellationToken ct = default);
    Task<Product?> GetBySlugAsync(string slug, bool includeInactive = false, CancellationToken ct = default);
    Task<Product?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetProductsByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default);
    Task<IReadOnlyList<ProductCardView>> GetFeaturedAsync(int count = 8, CancellationToken ct = default);
    Task<IReadOnlyList<ProductCardView>> GetNewArrivalsAsync(int count = 8, CancellationToken ct = default);
    Task<IReadOnlyList<ProductCardView>> GetRelatedAsync(string productId, int count = 4, CancellationToken ct = default);
    Task<IReadOnlyList<ProductCardView>> ToCardViewsAsync(IReadOnlyList<Product> products, CancellationToken ct = default);
    Task<IReadOnlyList<Category>> GetCategoriesAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<Category?> GetCategoryBySlugAsync(string slug, CancellationToken ct = default);
}

public interface ICartService
{
    Task<CartView> GetCartViewAsync(string ownerId, CancellationToken ct = default);
    Task<int> GetItemCountAsync(string ownerId, CancellationToken ct = default);
    Task<Result> AddItemAsync(string ownerId, bool isAnonymous, string productId, string? variantId, int quantity, CancellationToken ct = default);
    Task<Result> SetQuantityAsync(string ownerId, string productId, string variantId, int quantity, CancellationToken ct = default);
    Task<Result> RemoveItemAsync(string ownerId, string productId, string variantId, CancellationToken ct = default);
    Task ClearAsync(string ownerId, CancellationToken ct = default);
    /// <summary>Moves an anonymous cart into the user's cart at login.</summary>
    Task MergeAsync(string anonymousOwnerId, string userId, CancellationToken ct = default);
    Task<Result> ApplyCouponAsync(string ownerId, string code, CancellationToken ct = default);
    Task RemoveCouponAsync(string ownerId, CancellationToken ct = default);
    Task<CartTotals> GetTotalsAsync(string ownerId, string? shippingMethodId = null, CancellationToken ct = default);
}

public interface IOrderService
{
    Task<Result<Order>> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct = default);
    Task<Order?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetOrdersForUserAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetAllAsync(OrderStatus? status = null, CancellationToken ct = default);
    Task<Result> AdvanceStatusAsync(string orderId, OrderStatus to, CancellationToken ct = default);
    Task<IReadOnlyList<(string Label, int Count)>> GetStatusCountsAsync(CancellationToken ct = default);
    Task<(int OrderCount, decimal Revenue, decimal AvgOrderValue, int LowStockCount)> GetDashboardStatsAsync(CancellationToken ct = default);
}

public interface IPaymentService
{
    /// <summary>Mock gateway: validates card shape + Luhn, then applies Stripe-style test-card rules.</summary>
    Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct = default);
}

public interface IReviewService
{
    Task<IReadOnlyList<Review>> GetForProductAsync(string productId, CancellationToken ct = default);
    Task<IReadOnlyList<Review>> GetByUserAsync(string userId, CancellationToken ct = default);
    /// <summary>Adds or replaces the caller's review of the product (one per user per product).</summary>
    Task<Result> UpsertAsync(string userId, string userName, string productId, int rating, string title, string body, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, RatingSummary>> GetSummariesAsync(CancellationToken ct = default);
}

public interface IWishlistService
{
    /// <summary>Returns true when the product is in the wishlist after the toggle.</summary>
    Task<bool> ToggleAsync(string userId, string productId, CancellationToken ct = default);
    Task<bool> ContainsAsync(string userId, string productId, CancellationToken ct = default);
    Task<HashSet<string>> GetProductIdsAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<ProductCardView>> GetWishlistAsync(string userId, CancellationToken ct = default);
    Task<int> GetCountAsync(string userId, CancellationToken ct = default);
}

public interface ICouponService
{
    Task<CouponValidationResult> ValidateAsync(string? code, decimal subtotal, CancellationToken ct = default);
    Task RecordUsageAsync(string couponId, CancellationToken ct = default);
}

public interface IEmailService
{
    /// <summary>Mock sender — records the message so the admin Inbox can display it.</summary>
    Task SendAsync(string to, string subject, string body, EmailType type, CancellationToken ct = default);
}

public interface IAuthService
{
    Task<Result<AppUser>> RegisterAsync(string email, string fullName, string password, CancellationToken ct = default);
    Task<AppUser?> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default);
    Task<AppUser?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<AppUser>> GetAllAsync(CancellationToken ct = default);
}
