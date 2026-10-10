using System.Security.Claims;

namespace ECommerce.Web.Infrastructure;

/// <summary>Resolves the current cart owner: the logged-in user id, or the anonymous cart cookie id.</summary>
public static class CartOwner
{
    public const string CookieName = "nimbus_cart";

    /// <param name="createCookie">Set the anonymous cookie when missing (safe in handlers, avoided during GET rendering).</param>
    public static (string OwnerId, bool IsAnonymous) Resolve(this HttpContext http, bool createCookie = false)
    {
        var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId) && http.User.Identity?.IsAuthenticated == true)
            return (userId, false);

        if (http.Request.Cookies.TryGetValue(CookieName, out var existing) && !string.IsNullOrEmpty(existing))
            return (existing, true);

        var id = Guid.NewGuid().ToString("N");
        if (createCookie)
        {
            http.Response.Cookies.Append(CookieName, id, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true
            });
        }
        return (id, true);
    }
}
