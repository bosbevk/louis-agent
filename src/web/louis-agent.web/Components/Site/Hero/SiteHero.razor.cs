using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Hero;

/// <summary>
/// The first thing on a site's landing (F118, ADR 0106): a topic, the headline, a summary, a quiet note, the actions,
/// and something alive beside them when the site has it. A site may open the words with a piece of art, and may
/// have it all stand centred, with what was beside the words below them (F151.6). Every site's landing opens this
/// way and only the words differ, so the shape is the framework's and the site fills the slots. It marks itself as
/// the page's "Top" in the section index.
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
    [Parameter] public string? Topic { get; set; }

    /// <summary>The headline, the site's biggest words. A site gives this or <see cref="Lines"/>.</summary>
    [Parameter] public RenderFragment? Headline { get; set; }

    /// <summary>
    /// The headline as short sentences, each standing on a line of its own, so a two-beat line never breaks in the
    /// middle of a sentence on a wide screen. Used in place of <see cref="Headline"/>.
    /// </summary>
    [Parameter] public IReadOnlyList<string>? Lines { get; set; }

    /// <summary>The paragraph under the headline that says what this is.</summary>
    [Parameter] public RenderFragment? Summary { get; set; }

    /// <summary>A quiet line after the summary, such as who made it.</summary>
    [Parameter] public RenderFragment? Note { get; set; }

    /// <summary>The buttons and links under the words.</summary>
    [Parameter] public RenderFragment? Actions { get; set; }

    /// <summary>What stands beside the words: a live turn, a picture, nothing.</summary>
    [Parameter] public RenderFragment? Aside { get; set; }

    /// <summary>A piece of art that opens the words, above the topic, such as the agent's mark drawn large.</summary>
    [Parameter] public RenderFragment? Art { get; set; }

    /// <summary>The words stand centred, and what would stand beside them goes below.</summary>
    [Parameter] public bool Centred { get; set; }

    /// <summary>The headline's classes: the site's look, and a mark when it stands in lines.</summary>
    private string HeadlineClasses => Lines is null ? "chrome" : "chrome in-lines";

    /// <summary>Two columns only when there is something for the second one, centred when the site says so.</summary>
    private string Classes => (Aside, Centred) switch
    {
        (null, false) => "hero",
        (null, true) => "hero is-centred",
        (_, false) => "hero with-aside",
        _ => "hero with-aside is-centred"
    };
}
