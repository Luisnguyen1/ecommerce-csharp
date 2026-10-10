namespace ECommerce.Core.Entities;

/// <summary>Base type for all persisted entities. String ids (Guid "N" format) serialize cleanly to JSON.</summary>
public abstract class Entity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
}

public class Address
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Line1 { get; set; } = "";
    public string? Line2 { get; set; }
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Country { get; set; } = "United States";

    public override string ToString() =>
        $"{FullName}, {Line1}{(string.IsNullOrWhiteSpace(Line2) ? "" : ", " + Line2)}, {City}, {State} {PostalCode}, {Country}";
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";
}
