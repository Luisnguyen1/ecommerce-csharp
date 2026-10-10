using System.Security.Claims;
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using ECommerce.Web.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages.Account;

public class LoginModel(IAuthService auth, ICartService cartService) : PageModel
{
    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl) => ReturnUrl = returnUrl;

    public async Task<IActionResult> OnPostAsync(string? returnUrl)
    {
        ReturnUrl = returnUrl;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ModelState.AddModelError(string.Empty, "Enter your email and password.");
            return Page();
        }

        var user = await auth.ValidateCredentialsAsync(Email.Trim(), Password);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return Page();
        }

        await SignInWithCartMergeAsync(user);

        TempData["Toast.Message"] = $"Welcome back, {user.FullName.Split(' ', 2)[0]}!";
        TempData["Toast.Icon"] = "check-circle";
        TempData["Toast.Type"] = "success";

        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : Redirect("/");
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
