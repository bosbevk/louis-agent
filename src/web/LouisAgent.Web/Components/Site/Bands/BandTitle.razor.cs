using LouisAgent.Web.Components.Site.Addresses;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Bands;

/// <summary>
/// A band's heading (FX125). In the band at the top of a page it is the page's one h1, the main heading a search engine
/// and a screen reader look for; in a band that is a part of a section it is that part's h3 (F169.2); in any other
/// band, or outside one, it is a section's h2. A page writes it where its heading stands, so the layout around it
/// stays the page's own.
/// </summary>
/// <remarks>
/// The band hands itself down by cascade and the title asks it where it is, the same Registry shape as the page's
/// areas. Which band heads the page is the address rule's, not the order bands draw in: the band at the top is the
/// page itself (<see cref="SiteAddresses.Top"/>, ADR 0107). Blazor draws a band inside a component after the bands
/// beside it, and the host's prerender draws a page without its frame, so "the first band to draw" would hand the h1
/// to the wrong band; the address can't.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0107-every-section-has-an-address.md">ADR 0107: every section has an address</seealso>
public partial class BandTitle
{
    /// <summary>The band this title heads; none when it is drawn outside a band.</summary>
    [CascadingParameter]
    public Band? Band { get; set; }

    /// <summary>The site's own classes for the heading's look.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>The heading's words.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Whether this heads the page: it stands in the band at the page's top.</summary>
    private bool HeadsThePage => Band?.Id == SiteAddresses.Top;

    /// <summary>Whether this heads a part of a section: its band says which section it belongs to (F169.2).</summary>
    private bool IsAPart => !string.IsNullOrWhiteSpace(Band?.PartOf);

    /// <summary>
    /// A part's classes: the framework's <c>part-title</c> first, which the docs layout sizes, then the site's own.
    /// </summary>
    private string PartClasses => string.IsNullOrWhiteSpace(Class) ? PartTitle : $"{PartTitle} {Class.Trim()}";

    /// <summary>
    /// The framework's class on a part's title, so a rule can tell it from the other h3s a band holds.
    /// </summary>
    private const string PartTitle = "part-title";
}
