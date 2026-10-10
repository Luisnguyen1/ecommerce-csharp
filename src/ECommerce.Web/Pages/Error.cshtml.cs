using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Code { get; set; } = 500;

    public string? RequestId { get; private set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public bool IsNotFound => Code == 404;

    public void OnGet()
    {
        if (Code < 400 || Code > 599)
            Code = 500;

        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        ViewData["Title"] = IsNotFound ? "Page not found" : $"Error {Code}";
    }
}
