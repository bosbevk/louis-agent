using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Controls.Inputs;

/// <summary>A field a visitor types one line into, drawn as the theme's field: its edge lights in focus.</summary>
/// <remarks>The design page's field (FX162), configured by a site with its hint and its kind (ADR 0125).</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class PillField
{
    /// <summary>The hint in the empty field, and what a screen reader hears.</summary>
    [Parameter, EditorRequired] public string Placeholder { get; set; } = "";

    /// <summary>The input's kind, such as "email"; plain text by default.</summary>
    [Parameter] public string Kind { get; set; } = "text";
}
