using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages;

public class PrivacyModel : PageModel
{
    public void OnGet()
    {
        ViewData["Title"] = "Privacy";
    }
}
