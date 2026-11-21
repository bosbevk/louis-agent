using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Bands;

/// <summary>
/// One named area of a site's page (F117): a section with an id the framework insists on, a topic and the content. A
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
    [Parameter] public string? Topic { get; set; }

    /// <summary>
    /// The index's word for this band when the topic is too long for the margin; else the topic, else the id.
    /// </summary>
    [Parameter] public string? Short { get; set; }

    /// <summary>The site's own classes for this band's look, after <c>band</c>; none for a plain band.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>The band's content, under the topic.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>The band's heading, its first words in the theme's colour; none for a band that writes its own.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>The summary under the heading, in Markdown (ADR 0125); none for a band without one.</summary>
    [Parameter] public string? Summary { get; set; }

    /// <summary>A quiet line after the band's content, in Markdown, such as a link to more; none by default.</summary>
    [Parameter] public string? Note { get; set; }

    /// <summary>
    /// The id of the section this band is a part of (F169.2), such as one family of the Overview's whole; none for a
    /// band that is a section of its own. A part's title is the section's h3, not an h2 of its own.
    /// </summary>
    [Parameter] public string? PartOf { get; set; }

    /// <summary>
    /// Whether the frame may page the band sideways when it holds more than a screen (F118); a band whose content is
    /// one piece, such as a demo that plays, says false and stays whole (F122).
    /// </summary>
    [Parameter] public bool Paged { get; set; } = true;

    /// <summary>The section's mark for the frame's module: present only on a band that stays whole.</summary>
    private string? Whole => Paged ? null : "";

    /// <summary>The section's classes: always a band, then the site's.</summary>
    private string Classes => string.IsNullOrWhiteSpace(Class) ? "band" : $"band {Class.Trim()}";

    /// <summary>Puts the band on the page's list, by its short label, its topic or its id.</summary>
    protected override void OnParametersSet() => Areas?.Add(Id, Short ?? Topic ?? Id);

    /// <summary>Takes the band off the list when its page goes.</summary>
    public void Dispose() => Areas?.Remove(Id);
}
