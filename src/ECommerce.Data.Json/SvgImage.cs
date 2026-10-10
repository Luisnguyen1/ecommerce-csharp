using System.Text;

namespace ECommerce.Data.Json;

/// <summary>Generates the neutral placeholder image used when a product has no photo (e.g. added via admin).</summary>
public static class SvgImage
{
    public static void EnsurePlaceholder(string webRoot)
    {
        var dir = Path.Combine(webRoot, "images", "catalog");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "placeholder.svg");
        if (File.Exists(file)) return;

        var svg = """
<svg width="800" height="800" viewBox="0 0 800 800" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <linearGradient id="g" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#71717a"/>
      <stop offset="100%" stop-color="#a1a1aa"/>
    </linearGradient>
  </defs>
  <rect width="800" height="800" fill="url(#g)"/>
  <circle cx="650" cy="150" r="220" fill="#ffffff" opacity="0.08"/>
  <circle cx="110" cy="690" r="160" fill="#ffffff" opacity="0.06"/>
  <circle cx="180" cy="120" r="6" fill="#ffffff" opacity="0.35"/>
  <text x="400" y="480" font-size="260" text-anchor="middle" font-family="Segoe UI Emoji, Noto Color Emoji, sans-serif">📦</text>
</svg>
""";
        File.WriteAllText(file, svg, Encoding.UTF8);
    }
}
