using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Staging.Windows;

/// <summary>
/// A window's body (F134.1): panes of work, side by side. An agent at work has no website and no visitor, so what
/// its window shows is the work: the job it was given, what it did, who it handed on to.
/// </summary>
/// <remarks>
/// The second of the two bodies the framework offers for a <see cref="FramedWindow"/>. A pane is a
/// <see cref="WorkPane"/>, a heading over plain lines, so a site fills a window with data and no markup.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public partial class WorkBody
{
    /// <summary>The panes, side by side.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<WorkPane> Panes { get; set; } = [];

    /// <summary>How many panes there are, for the stylesheet to share the width between.</summary>
    private string Columns => FormattableString.Invariant($"--panes: {Panes.Count};");
}
