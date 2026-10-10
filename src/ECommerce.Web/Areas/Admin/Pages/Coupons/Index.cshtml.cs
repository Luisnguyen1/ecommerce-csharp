using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Areas.Admin.Pages.Coupons;

public class IndexModel : PageModel
{
    private readonly IRepository<Coupon> _coupons;

    public IndexModel(IRepository<Coupon> coupons)
    {
        _coupons = coupons;
    }

    [BindProperty]
    public CouponInputModel Input { get; set; } = new();

    public IReadOnlyList<Coupon> Coupons { get; private set; } = [];

    public async Task OnGetAsync() => Coupons = await LoadAsync();

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var code = (Input.Code ?? "").Trim().ToUpperInvariant();

        if (code.Length == 0)
            return await FailAsync("Code is required.");
        if (Input.Value <= 0)
            return await FailAsync("Value must be greater than zero.");
        if (Input.DiscountType == DiscountType.Percent && Input.Value > 100)
            return await FailAsync("A percent discount cannot exceed 100.");

        var all = await LoadAsync();
        if (all.Any(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            return await FailAsync($"Coupon \u201C{code}\u201D already exists.");

        await _coupons.AddAsync(new Coupon
        {
            Code = code,
            Description = Input.Description?.Trim() ?? "",
            DiscountType = Input.DiscountType,
            Value = Input.Value,
            MinOrderAmount = Input.MinOrderAmount,
            ExpiresAt = Input.ExpiresAt is { } expires ? DateTime.SpecifyKind(expires, DateTimeKind.Utc) : null,
            UsageLimit = Input.UsageLimit,
            IsActive = Input.IsActive
        });

        Toast($"Coupon \u201C{code}\u201D created.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(string id)
    {
        var coupon = await _coupons.GetByIdAsync(id);
        if (coupon is null)
        {
            Toast("Coupon not found.", isError: true);
            return RedirectToPage();
        }

        coupon.IsActive = !coupon.IsActive;
        await _coupons.UpdateAsync(coupon);

        Toast($"Coupon \u201C{coupon.Code}\u201D is now {(coupon.IsActive ? "active" : "inactive")}.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        await _coupons.RemoveAsync(id);
        Toast("Coupon deleted.");
        return RedirectToPage();
    }

    private async Task<IReadOnlyList<Coupon>> LoadAsync()
    {
        var all = await _coupons.GetAllAsync();
        Coupons = all
            .OrderByDescending(c => c.IsActive)
            .ThenBy(c => c.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return Coupons;
    }

    private async Task<IActionResult> FailAsync(string message)
    {
        await LoadAsync();
        Toast(message, isError: true);
        return Page();
    }

    private void Toast(string message, bool isError = false)
    {
        TempData["Toast.Message"] = message;
        TempData["Toast.Icon"] = isError ? "alert-circle" : "check-circle";
        TempData["Toast.Type"] = isError ? "error" : "success";
    }
}

public class CouponInputModel
{
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public DiscountType DiscountType { get; set; } = DiscountType.Percent;
    public decimal Value { get; set; }
    public decimal MinOrderAmount { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? UsageLimit { get; set; }
    public bool IsActive { get; set; } = true;
}
