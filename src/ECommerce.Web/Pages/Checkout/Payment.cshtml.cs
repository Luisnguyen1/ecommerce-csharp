using System.Security.Claims;
using System.Text.Json;
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Models;
using ECommerce.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages.Checkout;

public class PaymentModel(
    ICartService cartService,
    IPaymentService paymentService,
    IOrderService orderService) : PageModel
{
    private const string StateKey = "CheckoutState";
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    [BindProperty] public string CardHolder { get; set; } = "";
    [BindProperty] public string CardNumber { get; set; } = "";
    [BindProperty] public string Expiry { get; set; } = "";
    [BindProperty] public string Cvc { get; set; } = "";

    public CheckoutState State { get; set; } = new(new Address(), Shipping.StandardId);
    public CartView Cart { get; set; } = new() { Lines = [] };
    public CartTotals Totals { get; set; } = new();
    public ShippingOption ShippingOption { get; set; } = Shipping.Resolve(null, 0)!;

    public async Task<IActionResult> OnGetAsync()
    {
        // Peek keeps the state available for the POST that follows.
        if (!TryReadState(TempData.Peek(StateKey) as string))
            return RedirectToPage("/Checkout/Index");

        var (ownerId, _) = HttpContext.Resolve();
        if (!await LoadViewAsync(ownerId))
            return RedirectToPage("/Checkout/Index");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Reading TempData consumes it — keep the raw payload around to restore on failure.
        var raw = TempData[StateKey] as string;
        if (!TryReadState(raw))
            return RedirectToPage("/Checkout/Index");

        var (ownerId, _) = HttpContext.Resolve(createCookie: true);
        if (!await LoadViewAsync(ownerId))
            return CartEmpty();

        if (string.IsNullOrWhiteSpace(CardHolder))
            ModelState.AddModelError(nameof(CardHolder), "Cardholder name is required.");
        if (string.IsNullOrWhiteSpace(CardNumber))
            ModelState.AddModelError(nameof(CardNumber), "Card number is required.");
        if (string.IsNullOrWhiteSpace(Expiry))
            ModelState.AddModelError(nameof(Expiry), "Expiry is required.");
        if (string.IsNullOrWhiteSpace(Cvc))
            ModelState.AddModelError(nameof(Cvc), "Security code is required.");
        if (!ModelState.IsValid)
        {
            TempData[StateKey] = raw;
            return Page();
        }

        var payment = await paymentService.ChargeAsync(new PaymentRequest
        {
            CardNumber = CardNumber,
            CardHolder = CardHolder.Trim(),
            Expiry = Expiry.Trim(),
            Cvc = Cvc.Trim(),
            Amount = Totals.Total
        });

        if (!payment.Success)
        {
            ModelState.AddModelError(string.Empty, payment.Error ?? "Payment failed. Please try again.");
            TempData[StateKey] = raw;
            return Page();
        }

        var userId = User.Identity?.IsAuthenticated == true
            ? User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;

        var result = await orderService.PlaceOrderAsync(new PlaceOrderRequest
        {
            CartOwnerId = ownerId,
            ShippingAddress = State.Address,
            ShippingMethodId = State.ShippingMethodId,
            Payment = payment,
            UserId = userId
        });

        if (!result.Success)
        {
            TempData["Toast.Message"] = result.Error ?? "We couldn't place your order.";
            TempData["Toast.Icon"] = "alert-circle";
            TempData["Toast.Type"] = "error";
            return RedirectToPage("/Cart/Index");
        }

        return RedirectToPage("/Checkout/Success", new { orderNumber = result.Value!.OrderNumber });
    }

    private async Task<bool> LoadViewAsync(string ownerId)
    {
        Cart = await cartService.GetCartViewAsync(ownerId);
        if (Cart.IsEmpty)
            return false;

        Totals = await cartService.GetTotalsAsync(ownerId, State.ShippingMethodId);
        ShippingOption = Shipping.Resolve(State.ShippingMethodId, Totals.Subtotal) ?? Shipping.Resolve(null, Totals.Subtotal)!;
        return true;
    }

    private bool TryReadState(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        try
        {
            var parsed = JsonSerializer.Deserialize<CheckoutState>(raw, JsonOpts);
            if (parsed is null)
                return false;
            State = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private RedirectToPageResult CartEmpty()
    {
        TempData["Toast.Message"] = "Your cart is empty — add something first.";
        TempData["Toast.Icon"] = "alert-circle";
        TempData["Toast.Type"] = "error";
        return RedirectToPage("/Cart/Index");
    }
}
