using System.Security.Claims;
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Areas.Admin.Pages.Users;

public class IndexModel : PageModel
{
    private readonly IAuthService _auth;
    private readonly IRepository<AppUser> _users;

    public IndexModel(IAuthService auth, IRepository<AppUser> users)
    {
        _auth = auth;
        _users = users;
    }

    public IReadOnlyList<AppUser> Users { get; private set; } = [];

    public string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    public bool IsSelf(AppUser user) => user.Id == CurrentUserId;

    public async Task OnGetAsync() => Users = await _auth.GetAllAsync();

    public async Task<IActionResult> OnPostSetRoleAsync(string id, string role)
    {
        if (role != Roles.Admin && role != Roles.Customer)
        {
            Toast("Unknown role.", isError: true);
            return RedirectToPage();
        }

        if (id == CurrentUserId)
        {
            Toast("You cannot change your own role.", isError: true);
            return RedirectToPage();
        }

        var user = await _users.GetByIdAsync(id);
        if (user is null)
        {
            Toast("User not found.", isError: true);
            return RedirectToPage();
        }

        user.Role = role;
        await _users.UpdateAsync(user);

        Toast($"\"{user.FullName}\" is now {(role == Roles.Admin ? "an admin" : "a customer")}.");
        return RedirectToPage();
    }

    private void Toast(string message, bool isError = false)
    {
        TempData["Toast.Message"] = message;
        TempData["Toast.Icon"] = isError ? "alert-circle" : "check-circle";
        TempData["Toast.Type"] = isError ? "error" : "success";
    }
}
