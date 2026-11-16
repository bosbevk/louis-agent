using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Text;

/// <summary>
/// Opens a part of a page with its heading and a summary under it. A site gives the words; the look is the theme's.
/// </summary>
/// <remarks>
/// One of the controls a site configures instead of writing HTML (ADR 0125, Louis: "all of H3 and p in a control"),
/// so a heading and its summary look the same on every page and every site, and change with the theme.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class Intro
{
    /// <summary>The heading.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = "";

    /// <summary>The summary under it, in Markdown; none for a heading alone.</summary>
    [Parameter] public string? Summary { get; set; }
}
