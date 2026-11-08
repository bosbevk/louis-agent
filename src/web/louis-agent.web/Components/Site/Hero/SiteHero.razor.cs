using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Hero;

/// <summary>
/// The first thing on a site's landing (F118, ADR 0106): a kicker, the headline, a lede, a quiet note, the actions,
/// and something alive beside them when the site has it. Every site's landing opens this way and only the words
/// differ, so the shape is the framework's and the site fills the slots. It marks itself as the page's "Top" in the
/// section index.
/// </summary>
/// <remarks>
/// Template Method, in Razor (Gang of Four), as <see cref="SiteShell"/> is: the layout is fixed here and a site
/// fills the steps that differ through slots the compiler checks. A slot left empty draws nothing, and with no aside
/// the text takes the whole width, so a plain landing needs no second column.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0106-the-landing-is-a-business-website-from-the-framework.md">ADR 0106: the landing is a business website from the framework</seealso>
public partial class SiteHero
{
    /// <summary>The area's id, what the index and the guide name it by.</summary>
    [Parameter] public string Id { get; set; } = "top";

    /// <summary>The index's word for it.</summary>
    [Parameter] public string Label { get; set; } = "Top";

    /// <summary>The small mono line over the headline.</summary>
    [Parameter] public string? Kicker { get; set; }

    /// <summary>The headline, the site's biggest words.</summary>
    [Parameter, EditorRequired] public RenderFragment? Headline { get; set; }

    /// <summary>The paragraph under the headline that says what this is.</summary>
    [Parameter] public RenderFragment? Lede { get; set; }

    /// <summary>A quiet line after the lede, such as who made it.</summary>
    [Parameter] public RenderFragment? Note { get; set; }

    /// <summary>The buttons and links under the words.</summary>
    [Parameter] public RenderFragment? Actions { get; set; }

    /// <summary>What stands beside the words: a live turn, a picture, nothing.</summary>
    [Parameter] public RenderFragment? Aside { get; set; }

    /// <summary>Two columns only when there is something for the second one.</summary>
    private string Classes => Aside is null ? "hero" : "hero with-aside";
}
