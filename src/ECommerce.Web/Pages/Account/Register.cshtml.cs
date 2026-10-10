using System.Security.Claims;
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using ECommerce.Web.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages.Account;

public class RegisterModel(IAuthService auth, ICartService cartService, IEmailService emailService) : PageModel
{
    [BindProperty] public string FullName { get; set; } = "";
    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    [BindProperty] public string ConfirmPassword { get; set; } = "";

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(FullName))
            ModelState.AddModelError(nameof(FullName), "Your name is required.");
        if (string.IsNullOrWhiteSpace(Email))
            ModelState.AddModelError(nameof(Email), "Email is required.");
        if (Password.Length < 8)
            ModelState.AddModelError(nameof(Password), "Password must be at least 8 characters.");
        if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
            ModelState.AddModelError(nameof(ConfirmPassword), "Passwords do not match.");

        if (!ModelState.IsValid)
            return Page();

        var result = await auth.RegisterAsync(Email.Trim(), FullName.Trim(), Password);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not create your account.");
            return Page();
        }

        var user = result.Value!;

        await emailService.SendAsync(
            user.Email,
            "Welcome to Nimbus 👋",
            $"Hi {user.FullName},\n\nWelcome to Nimbus! Your account is ready — browse the catalog, save favorites to your wishlist, and try a mock checkout with test card 4242 4242 4242 4242.\n\nHappy shopping!\nThe Nimbus team",
            EmailType.Welcome);

        await SignInWithCartMergeAsync(user);

        TempData["Toast.Message"] = "Account created — welcome!";
        TempData["Toast.Icon"] = "check-circle";
        TempData["Toast.Type"] = "success";

        return Redirect("/");
    }

    private async Task SignInWithCartMergeAsync(AppUser user)
    {
        // Resolve() must run while the request is still anonymous to read the cart cookie.
        var (anonId, _) = HttpContext.Resolve();
        await SignInAsync(user);
        await cartService.MergeAsync(anonId, user.Id);
    }

    private async Task SignInAsync(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14) });
    }
}
