using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages.Account;

public class LogoutModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Index");

    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync();

        TempData["Toast.Message"] = "Signed out";
        TempData["Toast.Icon"] = "log-out";
        TempData["Toast.Type"] = "success";

        return RedirectToPage("/Index");
    }
}
