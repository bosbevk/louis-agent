using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Actions;

/// <summary>Stands a few buttons side by side, wrapping onto a second row on a narrow screen.</summary>
/// <remarks>
/// One of the controls a site configures instead of writing HTML (ADR 0125): a band lays what it holds one under the
/// other, so a section's ways on, such as asking the agent or the contact page, stand in this row.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class ButtonRow
{
    /// <summary>The buttons, in order.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
