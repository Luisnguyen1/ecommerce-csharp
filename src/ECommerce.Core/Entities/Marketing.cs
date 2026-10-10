namespace ECommerce.Core.Entities;

public enum DiscountType
{
    Percent,
    Fixed
}

public class Coupon : Entity
{
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public DiscountType DiscountType { get; set; } = DiscountType.Percent;
    /// <summary>Percent (1-100) or fixed amount, depending on DiscountType.</summary>
    public decimal Value { get; set; }
    public decimal MinOrderAmount { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? UsageLimit { get; set; }
    public int TimesUsed { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum EmailType
{
    Welcome,
    OrderConfirmation,
    OrderShipped,
    OrderDelivered,
    OrderCancelled
}

/// <summary>Mock mailbox — the demo's IEmailService "sends" by writing records an admin can read.</summary>
public class EmailMessage : Entity
{
    public string To { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public EmailType Type { get; set; } = EmailType.Welcome;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
