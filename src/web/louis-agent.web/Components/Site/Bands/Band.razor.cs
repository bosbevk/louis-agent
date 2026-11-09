using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Bands;

/// <summary>
/// One named area of a site's page (F117): a section with an id the framework insists on, a kicker and the content. A
/// page made of bands has every area named, so the guide can send a visitor to one (the <c>site</c> pack's ShowPage)
/// and the section index lists them: the band puts itself on the page's list as it draws and takes itself off as it
/// goes (F118), so no site writes an index by hand.
/// </summary>
/// <remarks>
/// A required parameter where a site used to write an id by hand, or forget to (Louis: "all pages must have areas"):
/// the rule lives in the one place every band passes through, and a site's test on real pages stays as the proof.
/// The list is the frame's, reached by cascade, so a band drawn alone (a test, a page off the frame) still works and
/// simply isn't indexed.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0106-the-landing-is-a-business-website-from-the-framework.md">ADR 0106: the landing is a business website from the framework</seealso>
public partial class Band : IDisposable
{
    /// <summary>The page's list of areas, from the frame; none when the band is drawn outside a site's shell.</summary>
    [CascadingParameter] public PageAreas? Areas { get; set; }

    /// <summary>The area's name, in words: what a link or the guide names it by.</summary>
    [Parameter, EditorRequired] public string Id { get; set; } = "";

    /// <summary>
    /// The small mono label over the band's content; none for a band that is a strip or a closing call.
    /// </summary>
    [Parameter] public string? Kicker { get; set; }

    /// <summary>
    /// The index's word for this band when the kicker is too long for the margin; else the kicker, else the id.
    /// </summary>
    [Parameter] public string? Short { get; set; }

    /// <summary>The site's own classes for this band's look, after <c>band</c>; none for a plain band.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>The band's content, under the kicker.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Whether the frame may page the band sideways when it holds more than a screen (F118); a band whose content is
    /// one piece, such as a demo that plays, says false and stays whole (F122).
    /// </summary>
    [Parameter] public bool Paged { get; set; } = true;

    /// <summary>The section's mark for the frame's module: present only on a band that stays whole.</summary>
    private string? Whole => Paged ? null : "";

    /// <summary>The section's classes: always a band, then the site's.</summary>
    private string Classes => string.IsNullOrWhiteSpace(Class) ? "band" : $"band {Class.Trim()}";

    /// <summary>Puts the band on the page's list, by its short label, its kicker or its id.</summary>
    protected override void OnParametersSet() => Areas?.Add(Id, Short ?? Kicker ?? Id);

    /// <summary>Takes the band off the list when its page goes.</summary>
    public void Dispose() => Areas?.Remove(Id);
}
