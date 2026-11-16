using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Controls.Labels;

/// <summary>A small label in the code face: a plain one, the visitor's, or the agent's, each drawn by the theme.</summary>
/// <remarks>The design page's tags (FX162), configured by a site with its words and whose it is (ADR 0125).</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class Tag
{
    /// <summary>The label's words.</summary>
    [Parameter, EditorRequired] public string Text { get; set; } = "";

    /// <summary>Whose it is; plain by default.</summary>
    [Parameter] public TagKind Kind { get; set; }

    /// <summary>The label's classes for whose it is.</summary>
    private string Classes => Kind switch
    {
        TagKind.You => "tag tag-you",
        TagKind.Agent => "tag tag-agent",
        _ => "tag"
    };
}
