using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Controls.Labels;

/// <summary>A small pill that says what is happening, such as "Live", with a lit dot before its words.</summary>
/// <remarks>The design page's status (FX162), configured by a site with its words (ADR 0125).</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class StatusPill
{
    /// <summary>What is happening.</summary>
    [Parameter, EditorRequired] public string Text { get; set; } = "";
}
