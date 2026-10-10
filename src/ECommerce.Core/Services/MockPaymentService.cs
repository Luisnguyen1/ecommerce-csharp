using ECommerce.Core.Interfaces;
using ECommerce.Core.Models;

namespace ECommerce.Core.Services;

/// <summary>
/// Mock gateway with Stripe-style test card semantics:
///   4242 4242 4242 4242 (or any valid card)  -> approved
///   any number ending 0002                   -> declined
///   any number ending 9995                   -> insufficient funds
///   any number ending 0069                   -> expired card
/// </summary>
public class MockPaymentService : IPaymentService
{
    public Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct = default)
    {
        var number = new string(request.CardNumber.Where(char.IsDigit).ToArray());

        if (number.Length is < 13 or > 19 || !LuhnValid(number))
            return Task.FromResult(Fail("Invalid card number."));
        if (!ExpiryValid(request.Expiry, out var expiryError))
            return Task.FromResult(Fail(expiryError!));
        if (request.Cvc.Length is < 3 or > 4 || !request.Cvc.All(char.IsDigit))
            return Task.FromResult(Fail("Invalid security code."));
        if (request.Amount <= 0)
            return Task.FromResult(Fail("Invalid amount."));
        if (string.IsNullOrWhiteSpace(request.CardHolder))
            return Task.FromResult(Fail("Cardholder name is required."));

        if (number.EndsWith("0002")) return Task.FromResult(Fail("Your card was declined."));
        if (number.EndsWith("9995")) return Task.FromResult(Fail("Your card has insufficient funds."));
        if (number.EndsWith("0069")) return Task.FromResult(Fail("Your card has expired."));

        return Task.FromResult(new PaymentResult
        {
            Success = true,
            TransactionId = $"txn_{Guid.NewGuid():N}",
            CardLast4 = number[^4..]
        });
    }

    private static PaymentResult Fail(string error) => new() { Success = false, Error = error };

    private static bool LuhnValid(string number)
    {
        var sum = 0;
        var alt = false;
        for (var i = number.Length - 1; i >= 0; i--)
        {
            var digit = number[i] - '0';
            if (alt)
            {
                digit *= 2;
                if (digit > 9) digit -= 9;
            }
            sum += digit;
            alt = !alt;
        }
        return sum % 10 == 0;
    }

    private static bool ExpiryValid(string expiry, out string? error)
    {
        error = null;
        var parts = expiry.Trim().Split('/', 2);
        if (parts.Length != 2 || !int.TryParse(parts[0], out var month) || !int.TryParse(parts[1], out var year))
        {
            error = "Expiry must be in MM/YY format.";
            return false;
        }
        if (month is < 1 or > 12)
        {
            error = "Invalid expiry month.";
            return false;
        }
        if (year < 100) year += 2000;
        var expiresEndOfMonth = new DateTime(year, month, 1).AddMonths(1);
        if (expiresEndOfMonth <= DateTime.UtcNow)
        {
            error = "This card has expired.";
            return false;
        }
        return true;
    }
}
