namespace ECommerce.Core.Entities;

public enum OrderStatus
{
    Placed,
    Paid,
    Shipped,
    Delivered,
    Cancelled
}

public class Order : Entity
{
    public string OrderNumber { get; set; } = "";
    public string? UserId { get; set; }           // null for guest checkout
    public string Email { get; set; } = "";
    public OrderStatus Status { get; set; } = OrderStatus.Placed;
    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Address ShippingAddress { get; set; } = new();
    public string ShippingMethodId { get; set; } = "";
    public string ShippingMethodName { get; set; } = "";

    public string? CouponCode { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal Total { get; set; }

    public string? PaymentTransactionId { get; set; }
    public string? CardLast4 { get; set; }

    public List<OrderLine> Lines { get; set; } = [];
}

/// <summary>Immutable line snapshot — never join live product data after purchase.</summary>
public class OrderLine
{
    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string VariantName { get; set; } = "";
    public string Sku { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}
