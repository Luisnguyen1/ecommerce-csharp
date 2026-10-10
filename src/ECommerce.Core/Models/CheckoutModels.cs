using ECommerce.Core.Entities;

namespace ECommerce.Core.Models;

public class ShippingOption
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required decimal Fee { get; init; }
}

/// <summary>Shipping catalog + free-shipping rule. Standard shipping is free above the threshold.</summary>
public static class Shipping
{
    public const decimal FreeShippingThreshold = 75m;
    public const decimal StandardFee = 5.99m;
    public const decimal ExpressFee = 14.99m;

    public static readonly string StandardId = "standard";
    public static readonly string ExpressId = "express";

    public static IReadOnlyList<ShippingOption> ForSubtotal(decimal subtotal) =>
    [
        new ShippingOption
        {
            Id = StandardId,
            Name = "Standard Shipping",
            Description = subtotal >= FreeShippingThreshold
                ? "Free — 3 to 5 business days"
                : $"{StandardFee:C0} — 3 to 5 business days",
            Fee = subtotal >= FreeShippingThreshold ? 0 : StandardFee
        },
        new ShippingOption
        {
            Id = ExpressId,
            Name = "Express Shipping",
            Description = $"{ExpressFee:C0} — 1 to 2 business days",
            Fee = ExpressFee
        }
    ];

    public static ShippingOption? Resolve(string? methodId, decimal subtotal) =>
        ForSubtotal(subtotal).FirstOrDefault(o => o.Id == methodId)
        ?? ForSubtotal(subtotal).FirstOrDefault();
}

public class PlaceOrderRequest
{
    public required string CartOwnerId { get; init; }
    public required Address ShippingAddress { get; init; }
    public required string ShippingMethodId { get; init; }
    public required PaymentResult Payment { get; init; }
    public string? UserId { get; init; }
}

public class PaymentRequest
{
    public required string CardNumber { get; init; }
    public required string CardHolder { get; init; }
    /// <summary>"MM/YY"</summary>
    public required string Expiry { get; init; }
    public required string Cvc { get; init; }
    public required decimal Amount { get; init; }
}

public class PaymentResult
{
    public bool Success { get; init; }
    public string? TransactionId { get; init; }
    public string? CardLast4 { get; init; }
    public string? Error { get; init; }
}
