using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Bands;

/// <summary>
/// One named area of a site's page (F117): a section with an id the framework insists on, a kicker and the content. A
/// page made of bands has every area named, so the guide can send a visitor to one (the <c>site</c> pack's ShowPage)
/// and a section index can list them.
/// </summary>
/// <remarks>
/// A required parameter where a site used to write an id by hand, or forget to (Louis: "all pages must have areas"):
/// the rule lives in the one place every band passes through, and a site's test on real pages stays as the proof.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0106-the-landing-is-a-business-website-from-the-framework.md">ADR 0106: the landing is a business website from the framework</seealso>
public partial class Band
{
    /// <summary>The area's name, in words: what a link or the guide names it by.</summary>
    [Parameter, EditorRequired] public string Id { get; set; } = "";

    /// <summary>The small mono label over the band's content; none for a band that is a strip or a closing call.</summary>
    [Parameter] public string? Kicker { get; set; }

    /// <summary>The site's own classes for this band's look, after <c>band</c>; none for a plain band.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>The band's content, under the kicker.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>The section's classes: always a band, then the site's.</summary>
    private string Classes => string.IsNullOrWhiteSpace(Class) ? "band" : $"band {Class.Trim()}";
}
