using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Staging.Windows;

/// <summary>
/// A framed window (F134.1): a bar like a browser's with an address word, then whatever body it is given. It shows a
/// thing as the thing itself, a website in its window, and knows nothing of what the thing is.
/// </summary>
/// <remarks>
/// A wrapper with one slot (Louis: "like cool components we could reuse later in other projects"). The framework
/// offers two bodies to put in it, <see cref="SiteBody"/> and <see cref="WorkBody"/>, and a site may write its own.
/// Whether it stands in the light is told to it, since only the page knows which window is forward.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public partial class FramedWindow
{
    /// <summary>The word in the bar, where a browser shows an address.</summary>
    [Parameter, EditorRequired]
    public string Address { get; set; } = "";

    /// <summary>Whether the window stands in the light.</summary>
    [Parameter]
    public bool Lit { get; set; }

    /// <summary>What the window shows.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The window's classes: itself, and lit when it stands in the light.</summary>
    private string Classes => Lit ? "framed-window is-lit" : "framed-window";
}
