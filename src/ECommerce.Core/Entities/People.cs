namespace ECommerce.Core.Entities;

public class AppUser : Entity
{
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = Roles.Customer;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Address> SavedAddresses { get; set; } = [];
}

public class Review : Entity
{
    public string ProductId { get; set; } = "";
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public int Rating { get; set; } // 1..5
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class WishlistItem : Entity
{
    public string UserId { get; set; } = "";
    public string ProductId { get; set; } = "";
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
