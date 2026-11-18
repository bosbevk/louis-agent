using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Hero;

/// <summary>
/// A person's hero, as a CV opens: the name, a summary, a quiet line of where they are, the ways to reach them, and
/// their photo beside it all, above the words on a phone. On paper the buttons can't be pressed, so the row a site
/// gives for paper, such as each link as its QR code, stands under both columns instead.
/// </summary>
/// <remarks>
/// Template Method, in Razor, as <see cref="SiteHero"/> is: the layout is fixed here and a site fills the slots. A
/// landing's hero sells with huge words and something alive beside them; a person's stands smaller beside a photo.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class ProfileHero
{
    /// <summary>The person's name, the page's heading.</summary>
    [Parameter, EditorRequired] public RenderFragment? Name { get; set; }

    /// <summary>The paragraph under the name.</summary>
    [Parameter] public RenderFragment? Summary { get; set; }

    /// <summary>A quiet line, such as where they live and the languages they speak.</summary>
    [Parameter] public RenderFragment? Note { get; set; }

    /// <summary>The ways to reach them, as buttons; left off on paper.</summary>
    [Parameter] public RenderFragment? Actions { get; set; }

    /// <summary>What stands under the buttons, such as a small form that changes one.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>The photo's address; none draws no photo.</summary>
    [Parameter] public string? Photo { get; set; }

    /// <summary>What the photo shows, for who can't see it.</summary>
    [Parameter] public string PhotoText { get; set; } = "";

    /// <summary>The row drawn only on paper, under both columns.</summary>
    [Parameter] public RenderFragment? Paper { get; set; }
}
