using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Areas.Admin.Pages.Inbox;

public class IndexModel : PageModel
{
    private readonly IRepository<EmailMessage> _emails;

    public IndexModel(IRepository<EmailMessage> emails)
    {
        _emails = emails;
    }

    public IReadOnlyList<EmailMessage> Emails { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Emails = (await _emails.GetAllAsync())
            .OrderByDescending(m => m.SentAt)
            .ToList();
    }

    public async Task<IActionResult> OnPostClearAllAsync()
    {
        foreach (var email in await _emails.GetAllAsync())
        {
            await _emails.RemoveAsync(email.Id);
        }

        TempData["Toast.Message"] = "Inbox cleared.";
        TempData["Toast.Icon"] = "check-circle";
        TempData["Toast.Type"] = "success";
        return RedirectToPage();
    }
}
