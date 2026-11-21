using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Controls.Labels;

/// <summary>A well cut into the glass that holds something to read, in the code face, such as a note or a value.</summary>
/// <remarks>The design page's well (FX162), configured by a site with its words in Markdown (ADR 0125).</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class Well
{
    /// <summary>What it holds, in Markdown.</summary>
    [Parameter, EditorRequired] public string Text { get; set; } = "";
}
