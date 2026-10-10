using ECommerce.Core.Entities;

namespace ECommerce.Core.Models;

public class CartLineView
{
    public required string ProductId { get; init; }
    public required string VariantId { get; init; }
    public required string ProductName { get; init; }
    public required string ProductSlug { get; init; }
    public required string VariantName { get; init; }
    public required string ImageUrl { get; init; }
    public required decimal UnitPrice { get; init; }
    public required int Quantity { get; init; }
    public required int StockAvailable { get; init; }
    public decimal LineTotal => UnitPrice * Quantity;
}

public class CartView
{
    public required IReadOnlyList<CartLineView> Lines { get; init; }
    public string? AppliedCouponCode { get; init; }
    public int ItemCount { get; init; }
    public bool IsEmpty => Lines.Count == 0;
}

public class CartTotals
{
    public decimal Subtotal { get; init; }
    public decimal Discount { get; init; }
    public string? CouponCode { get; init; }
    public string? CouponError { get; init; }
    /// <summary>Shipping fee for a given method; computed with the free-shipping rule applied.</summary>
    public decimal ShippingFee { get; init; }
    public string? ShippingMethodId { get; init; }
    public decimal Total => Math.Max(0, Subtotal - Discount) + ShippingFee;
    public decimal TaxableBase => Math.Max(0, Subtotal - Discount);
}
