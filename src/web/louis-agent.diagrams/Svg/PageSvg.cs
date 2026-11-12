using System.Xml.Linq;

namespace LouisAgent.Diagrams.Svg;

/// <summary>
/// Turns a drawing's file into one a page can hold inline (F127.4.1). A style inside an inline SVG is the whole
/// page's, so each one is wrapped in <c>@scope</c>, which the browser keeps to the drawing; and an arrowhead no arrow
/// uses is dropped, so two drawings on one page hold no id twice.
/// </summary>
/// <remarks>
/// Inline with <c>@scope</c>, the pick of three (an <c>img</c> keeps the page out, so a business's brand colours and
/// fonts can't reach a ticket; a shadow root needs a script per drawing and hides it from the tests): the page's
/// <c>--ticket-*</c> variables still reach the drawing (Q3), and the drawing's rules reach nothing of the page's.
/// </remarks>
internal static class PageSvg
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    /// <summary>The page's copy of a drawing's file.</summary>
    public static string Of(string svg)
    {
        var root = XElement.Parse(svg);
        foreach (var style in root.Elements(Svg + "style")) style.Value = $"@scope {{\n{style.Value}\n}}";

        // An arrowhead is only kept when something points with it.
        bool arrows = root.Descendants().Any(shape =>
            shape.Attribute("marker-end") is not null || shape.Attribute("marker-start") is not null);
        if (!arrows) root.Elements(Svg + "defs").Remove();

        return SvgWriter.Serialize(root);
    }
}
