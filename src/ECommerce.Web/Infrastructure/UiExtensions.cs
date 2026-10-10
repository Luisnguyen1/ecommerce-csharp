using System.Globalization;

namespace ECommerce.Web.Infrastructure;

public static class UiExtensions
{
    private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

    /// <summary>Formats money in USD regardless of server culture.</summary>
    public static string Money(this decimal value) => value.ToString("C", Us);

    public static string Money(this decimal? value) => value?.Money() ?? "—";

    /// <summary>"3 hours ago" / "Oct 4, 2026" style timestamps.</summary>
    public static string Friendly(this DateTime utc)
    {
        var local = utc.ToLocalTime();
        var delta = DateTimeOffset.Now - local;
        return delta.TotalMinutes < 1 ? "just now"
            : delta.TotalHours < 1 ? $"{(int)delta.TotalMinutes}m ago"
            : delta.TotalHours < 24 && local.Date == DateTimeOffset.Now.Date ? $"{(int)delta.TotalHours}h ago"
            : local.ToString("MMM d, yyyy", Us);
    }
}
