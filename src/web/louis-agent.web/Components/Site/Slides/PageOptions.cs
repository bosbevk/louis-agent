namespace LouisAgent.Web.Components.Site.Slides;

/// <summary>
/// What the page in view asks of the frame (F118): its own layout, when it wants one other than the site's. The frame
/// hands this down its cascade, a page's <see cref="PageLayoutChoice"/> fills it, and the frame redraws when it changes.
/// </summary>
/// <remarks>
/// The same shape as <see cref="Bands.PageAreas"/>: a small holder the parts fill and the frame listens to, so a page
/// tells the frame something without the frame knowing any page (Observer).
/// </remarks>
public sealed class PageOptions
{
    /// <summary>The page's own layout; none, and the site's holds.</summary>
    public PageLayout? Layout { get; private set; }

    /// <summary>Raised when the page's choice changes, so the frame redraws.</summary>
    public event Action? Changed;

    /// <summary>Takes the page's choice, or clears it with none.</summary>
    public void Choose(PageLayout? layout)
    {
        if (Layout == layout) return;
        Layout = layout;
        Changed?.Invoke();
    }
}
