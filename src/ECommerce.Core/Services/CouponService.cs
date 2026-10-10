using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Models;

namespace ECommerce.Core.Services;

public class CouponService(IRepository<Coupon> coupons) : ICouponService
{
    public async Task<CouponValidationResult> ValidateAsync(string? code, decimal subtotal, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new CouponValidationResult { IsValid = false, Error = "Enter a coupon code." };

        var coupon = (await coupons.GetAllAsync(ct))
            .FirstOrDefault(c => c.Code.Equals(code.Trim(), StringComparison.OrdinalIgnoreCase));

        if (coupon is null || !coupon.IsActive)
            return new CouponValidationResult { IsValid = false, Error = $"Coupon \"{code}\" does not exist." };
        if (coupon.ExpiresAt is { } expiry && expiry < DateTime.UtcNow)
            return new CouponValidationResult { IsValid = false, Error = "This coupon has expired." };
        if (coupon.UsageLimit is { } limit && coupon.TimesUsed >= limit)
            return new CouponValidationResult { IsValid = false, Error = "This coupon has reached its usage limit." };
        if (subtotal < coupon.MinOrderAmount)
            return new CouponValidationResult
            {
                IsValid = false,
                Error = $"This coupon requires a minimum order of {coupon.MinOrderAmount:C}."
            };

        var discount = coupon.DiscountType switch
        {
            DiscountType.Percent => Math.Round(subtotal * coupon.Value / 100m, 2),
            DiscountType.Fixed => Math.Min(coupon.Value, subtotal),
            _ => 0
        };

        return new CouponValidationResult { IsValid = true, Coupon = coupon, DiscountAmount = discount };
    }

    public async Task RecordUsageAsync(string couponId, CancellationToken ct = default)
    {
        var coupon = await coupons.GetByIdAsync(couponId, ct);
        if (coupon is null) return;
        coupon.TimesUsed++;
        await coupons.UpdateAsync(coupon, ct);
    }
}
