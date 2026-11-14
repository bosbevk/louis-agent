using LouisAgent.Diagrams.Icons;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Staging.Windows;

/// <summary>
/// A window's body (F134.1): a website with its chat beside it, as a small still picture of the two. The photo, the
/// name and the line stand for the website, and one question with its answer for the talk.
/// </summary>
/// <remarks>
/// One of the two bodies the framework offers for a <see cref="FramedWindow"/> (Louis's pick of three: two ready
/// bodies the site fills, over one record for everything or the real mock website shrunk). It holds no words of
/// its own: every one comes in, so the site says them and a test of the site can hold them to its recordings.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public partial class SiteBody
{
    /// <summary>The website's photo.</summary>
    [Parameter, EditorRequired]
    public string Photo { get; set; } = "";

    /// <summary>The website's name, as its mark.</summary>
    [Parameter, EditorRequired]
    public string Name { get; set; } = "";

    /// <summary>The line over the photo.</summary>
    [Parameter, EditorRequired]
    public string Line { get; set; } = "";

    /// <summary>A smaller line under it.</summary>
    [Parameter]
    public string? Note { get; set; }

    /// <summary>The words of the website's menu.</summary>
    [Parameter]
    public IReadOnlyList<string> Menu { get; set; } = [];

    /// <summary>The chat's icon.</summary>
    [Parameter, EditorRequired]
    public DiagramIcon Icon { get; set; }

    /// <summary>The chat's title, such as "Ask us".</summary>
    [Parameter, EditorRequired]
    public string Ask { get; set; } = "";

    /// <summary>A few words beside the title.</summary>
    [Parameter]
    public string? AskNote { get; set; }

    /// <summary>What the visitor asks.</summary>
    [Parameter, EditorRequired]
    public string Question { get; set; } = "";

    /// <summary>What the agent answers.</summary>
    [Parameter, EditorRequired]
    public string Answer { get; set; } = "";

    /// <summary>One thing the agent showed, under its answer.</summary>
    [Parameter]
    public RenderFragment? Shown { get; set; }

    /// <summary>The words in the box a visitor would type in.</summary>
    [Parameter]
    public string? Hint { get; set; }
}
