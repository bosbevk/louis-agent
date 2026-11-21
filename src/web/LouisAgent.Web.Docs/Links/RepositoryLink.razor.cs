using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Docs.Links;

/// <summary>
/// A link to another repository, such as a product built on the framework elsewhere, that sits beside the doc links
/// and looks like them (F166.4). A doc link knows its doc by its path here; this knows its whole address itself.
/// </summary>
/// <remarks>
/// Its own control rather than a second mode of <see cref="DocLink"/> (Louis's pick A of F166.4's finding 4), so each
/// knows its address in one way.
/// </remarks>
public partial class RepositoryLink
{
    /// <summary>The repository's whole web address.</summary>
    [Parameter, EditorRequired]
    public string Href { get; set; } = "";

    /// <summary>The link's words.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
