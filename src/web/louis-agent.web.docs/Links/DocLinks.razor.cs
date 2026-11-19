using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Docs.Links;

/// <summary>A section's doc links, together, as the line the section ends with.</summary>
public partial class DocLinks
{
    /// <summary>The section's doc links.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
