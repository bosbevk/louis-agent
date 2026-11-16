using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Actions;

/// <summary>
/// A button that asks the site's agent a question for the visitor, such as to take their details: the guide opens and
/// answers, as if the visitor had typed it.
/// </summary>
/// <remarks>
/// One of the controls a site configures instead of writing HTML (ADR 0125), drawn by the theme's <c>.btn</c> rules
/// as <see cref="LinkButton"/> is. The question rides on the button, and the widget's piece watcher, which already
/// hears the page's clicks, hands it on, so the button needs no script of its own.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class AskButton
{
    /// <summary>The button's words.</summary>
    [Parameter, EditorRequired] public string Text { get; set; } = "";

    /// <summary>What it asks the agent, in the visitor's words.</summary>
    [Parameter, EditorRequired] public string Question { get; set; } = "";

    /// <summary>The main button or the quiet one; the main one by default.</summary>
    [Parameter] public ButtonKind Kind { get; set; }

    /// <summary>The theme's button classes for its kind.</summary>
    private string Classes => Kind == ButtonKind.Quiet ? "btn btn-line" : "btn btn-fill";
}
