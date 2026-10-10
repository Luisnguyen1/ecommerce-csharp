using System.Security.Claims;
using System.Text.Json;
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Models;
using ECommerce.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages.Checkout;

/// <summary>Serialized to TempData between the address step and the payment step.</summary>
public sealed record CheckoutState(Address Address, string ShippingMethodId);

public class IndexModel(ICartService cartService, IAuthService authService) : PageModel
{
    private const string StateKey = "CheckoutState";
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    [BindProperty]
    public Address Input { get; set; } = new();

    [BindProperty]
    public string? ShippingMethodId { get; set; }

    public CartView Cart { get; set; } = new() { Lines = [] };
    public CartTotals Totals { get; set; } = new();
    public IReadOnlyList<ShippingOption> ShippingOptions { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var (ownerId, _) = HttpContext.Resolve();

        Cart = await cartService.GetCartViewAsync(ownerId);
        if (Cart.IsEmpty)
            return RedirectToCartEmpty();

        Totals = await cartService.GetTotalsAsync(ownerId);
        ShippingOptions = Shipping.ForSubtotal(Totals.Subtotal);
        ShippingMethodId ??= Shipping.StandardId;

        // Prefill from the signed-in user's profile / first saved address.
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = userId is null ? null : await authService.GetByIdAsync(userId);
            if (user is not null)
            {
                if (user.SavedAddresses.Count > 0)
                {
                    Input = Clone(user.SavedAddresses[0]);
                }
                else
                {
                    Input.FullName = user.FullName;
                    Input.Email = user.Email;
                }
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var (ownerId, _) = HttpContext.Resolve(createCookie: true);

        Cart = await cartService.GetCartViewAsync(ownerId);
        if (Cart.IsEmpty)
            return RedirectToCartEmpty();

        Totals = await cartService.GetTotalsAsync(ownerId);
        ShippingOptions = Shipping.ForSubtotal(Totals.Subtotal);

        if (string.IsNullOrWhiteSpace(Input.Country))
            Input.Country = "United States";

        ValidateRequired(nameof(Input.FullName), Input.FullName, "Full name is required.");
        ValidateRequired(nameof(Input.Email), Input.Email, "Email is required.");
        ValidateRequired(nameof(Input.Phone), Input.Phone, "Phone number is required.");
        ValidateRequired(nameof(Input.Line1), Input.Line1, "Address line 1 is required.");
        ValidateRequired(nameof(Input.City), Input.City, "City is required.");
        ValidateRequired(nameof(Input.State), Input.State, "State is required.");
        ValidateRequired(nameof(Input.PostalCode), Input.PostalCode, "Postal code is required.");

        var method = Shipping.Resolve(ShippingMethodId, Totals.Subtotal);
        if (method is null)
            ModelState.AddModelError(nameof(ShippingMethodId), "Choose a shipping method.");

        if (!ModelState.IsValid)
            return Page();

        TempData[StateKey] = JsonSerializer.Serialize(new CheckoutState(Input, method!.Id), JsonOpts);
        return RedirectToPage("/Checkout/Payment");
    }

    private void ValidateRequired(string key, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            ModelState.AddModelError($"Input.{key}", message);
    }

    private RedirectToPageResult RedirectToCartEmpty()
    {
        TempData["Toast.Message"] = "Your cart is empty — add something first.";
        TempData["Toast.Icon"] = "alert-circle";
        TempData["Toast.Type"] = "error";
        return RedirectToPage("/Cart/Index");
    }

    private static Address Clone(Address a) => new()
    {
        FullName = a.FullName,
        Email = a.Email,
        Phone = a.Phone,
        Line1 = a.Line1,
        Line2 = a.Line2,
        City = a.City,
        State = a.State,
        PostalCode = a.PostalCode,
        Country = a.Country
    };
}
