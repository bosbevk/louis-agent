using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Text;

/// <summary>Shows a few steps in their order, each numbered, with its title strong and then its words.</summary>
/// <remarks>
/// One of the controls a site configures instead of writing HTML (ADR 0125): a site's "three steps" read the same on
/// every page and every site.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class Steps
{
    /// <summary>The steps, in their order.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<Step> Items { get; set; } = [];
}
