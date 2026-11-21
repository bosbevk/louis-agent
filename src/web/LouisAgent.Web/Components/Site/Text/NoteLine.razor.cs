using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Text;

/// <summary>A quiet line after a part of a page, such as what is sold or a link to more, for a band with several.</summary>
/// <remarks>One of the controls a site configures instead of writing HTML (ADR 0125).</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class NoteLine
{
    /// <summary>The line, in Markdown.</summary>
    [Parameter, EditorRequired] public string Text { get; set; } = "";
}
