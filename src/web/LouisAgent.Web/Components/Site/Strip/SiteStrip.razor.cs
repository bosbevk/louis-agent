using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Strip;

/// <summary>
/// The strip that closes a site's page and asks the visitor to act (F118): a heading, a line of words and a button,
/// on a band of its own. Every business site ends this way, so the shape is the framework's and the words the site's.
/// </summary>
/// <remarks>
/// Template Method in Razor, as the hero is. The strip's colours are names a site sets (<c>--web-strip-bg</c>,
/// <c>--web-strip-text</c>, <c>--web-strip-muted</c>), so the framework holds no colour of its own and a site without
/// them gets a strip in its emphasis colour.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0106-the-landing-is-a-business-website-from-the-framework.md">ADR 0106: the landing is a business website from the framework</seealso>
public partial class SiteStrip
{
    /// <summary>The area's id, what the index and the guide name it by.</summary>
    [Parameter] public string Id { get; set; } = "contact";

    /// <summary>The index's word for it.</summary>
    [Parameter] public string Short { get; set; } = "Contact";

    /// <summary>The question or call the strip opens with.</summary>
    [Parameter, EditorRequired] public string Heading { get; set; } = "";

    /// <summary>The line under the heading.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>The button or link the visitor acts on.</summary>
    [Parameter] public RenderFragment? Action { get; set; }
}
