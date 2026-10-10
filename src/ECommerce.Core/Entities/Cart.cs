namespace ECommerce.Core.Entities;

/// <summary>Cart owned either by a logged-in user id or an anonymous cookie id.</summary>
public class Cart : Entity
{
    public string OwnerId { get; set; } = "";
    public bool IsAnonymous { get; set; }
    public string? AppliedCouponCode { get; set; }
    public List<CartItem> Items { get; set; } = [];
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class CartItem
{
    public string ProductId { get; set; } = "";
    public string VariantId { get; set; } = "";
    public int Quantity { get; set; } = 1;
    /// <summary>Unit price snapshot taken when the item was added.</summary>
    public decimal UnitPrice { get; set; }
}
