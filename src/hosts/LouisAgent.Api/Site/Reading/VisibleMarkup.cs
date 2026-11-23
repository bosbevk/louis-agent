using System.Text;
using LouisAgent.Api.Site.Places;

namespace LouisAgent.Api.Site.Reading;

/// <summary>
/// Markup with what a visitor never reads taken out: an element marked <c>hidden</c>, or hidden from a screen reader
/// with <c>aria-hidden="true"</c>, goes with everything inside it. What CSS hides can't be known from the markup, so
/// it stays.
/// </summary>
/// <remarks>
/// Louis's pick (F126.3.3), over reading every rendered word and over the site marking its CSS-hidden scenes too:
/// the browser's reading leaves these out, and the markup says so plainly. Only a page read for a message asks it;
/// the site's places index keeps every word for its search.
/// </remarks>
internal static class VisibleMarkup
{
    /// <summary>The markup without its hidden elements.</summary>
    public static string Of(string markup)
    {
        var visible = new StringBuilder();
        int at = 0;
        foreach (var tag in HtmlTag.All(markup))
        {
            // A tag inside an element already left out is passed over with it.
            if (tag.Start < at || !IsHidden(tag)) continue;
            visible.Append(markup[at..tag.Start]);
            at = EndOf(tag, markup);
        }

        return visible.Append(markup[at..]).ToString();
    }

    /// <summary>Whether the markup marks the element as never shown, or hidden from a screen reader.</summary>
    private static bool IsHidden(HtmlTag tag) =>
        tag.Has("hidden") || tag.Attributes.GetValueOrDefault("aria-hidden") == "true";

    /// <summary>Where the element ends: past its closing tag, or at the markup's end when it has none.</summary>
    private static int EndOf(HtmlTag tag, string markup)
    {
        int closing = tag.ContentEnd(markup);
        int close = closing < markup.Length ? markup.IndexOf('>', closing) : -1;
        return close < 0 ? markup.Length : close + 1;
    }
}
