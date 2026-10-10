using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ECommerce.Web.Infrastructure;

/// <summary>Renders a lucide icon inline: &lt;icon name="shopping-cart" class="size-4"/&gt;</summary>
[HtmlTargetElement("icon", Attributes = "name")]
public class IconTagHelper : TagHelper
{
    public string Name { get; set; } = "";
    public string? Class { get; set; }
    /// <summary>Fill the shapes with currentColor (e.g. a filled star or heart).</summary>
    public bool Fill { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var markup = Icons.Map.GetValueOrDefault(Name);
        if (markup is null)
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "svg";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("viewBox", "0 0 24 24");
        output.Attributes.SetAttribute("xmlns", "http://www.w3.org/2000/svg");
        output.Attributes.SetAttribute("fill", Fill ? "currentColor" : "none");
        output.Attributes.SetAttribute("stroke", "currentColor");
        output.Attributes.SetAttribute("stroke-width", "2");
        output.Attributes.SetAttribute("stroke-linecap", "round");
        output.Attributes.SetAttribute("stroke-linejoin", "round");
        output.Attributes.SetAttribute("aria-hidden", "true");
        if (!string.IsNullOrEmpty(Class))
            output.Attributes.SetAttribute("class", Class);
        output.Content.SetHtmlContent(markup);
    }
}
