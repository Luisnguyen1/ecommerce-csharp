using ECommerce.Core.Entities;

namespace ECommerce.Data.Json;

/// <summary>The whole database, one file. Collection names map entity types to properties here.</summary>
public class DatabaseFile
{
    public int SchemaVersion { get; set; } = 1;
    public List<Category> Categories { get; set; } = [];
    public List<Product> Products { get; set; } = [];
    public List<AppUser> Users { get; set; } = [];
    public List<Cart> Carts { get; set; } = [];
    public List<Order> Orders { get; set; } = [];
    public List<Review> Reviews { get; set; } = [];
    public List<WishlistItem> WishlistItems { get; set; } = [];
    public List<Coupon> Coupons { get; set; } = [];
    public List<EmailMessage> Emails { get; set; } = [];
}
