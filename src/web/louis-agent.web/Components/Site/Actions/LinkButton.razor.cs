using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Actions;

/// <summary>
/// A link that looks like one of the theme's buttons: the main one or the quiet one, small when asked. A link to a
/// place on the same page names its section, so the frame moves there.
/// </summary>
/// <remarks>
/// One of the controls a site configures instead of writing HTML (ADR 0125), so every button on every site is the
/// theme's, drawn by the framework's <c>.btn</c> rules.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class LinkButton
{
    /// <summary>Where the button goes.</summary>
    [Parameter, EditorRequired] public string Href { get; set; } = "";

    /// <summary>The button's words.</summary>
    [Parameter, EditorRequired] public string Text { get; set; } = "";

    /// <summary>The main button or the quiet one; the main one by default.</summary>
    [Parameter] public ButtonKind Kind { get; set; }

    /// <summary>The smaller size, as in a header.</summary>
    [Parameter] public bool Small { get; set; }

    /// <summary>The section on this page it moves to, by its id; none for a link to another page.</summary>
    [Parameter] public string? Section { get; set; }

    /// <summary>A mark before the words, such as a third party's logo; none by default.</summary>
    [Parameter] public RenderFragment? Mark { get; set; }

    /// <summary>Whether the link opens in a new tab, as a link to another site does; off by default.</summary>
    [Parameter] public bool NewTab { get; set; }

    /// <summary>The theme's button classes for its kind and size.</summary>
    private string Classes => Kind switch
    {
        ButtonKind.Quiet => "btn btn-line",
        ButtonKind.WhatsApp => "btn btn-whatsapp",
        ButtonKind.LinkedIn => "btn btn-linkedin",
        _ => "btn btn-fill"
    } + (Small ? " btn-small" : "");

    /// <summary>Where the link opens: a new tab when asked, else none, the same tab.</summary>
    private string? Target => NewTab ? "_blank" : null;

    /// <summary>A link in a new tab doesn't hand the page it leaves to the site it opens.</summary>
    private string? Rel => NewTab ? "noopener" : null;
}
