using LouisAgent.Site.Resumes;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Admin;

/// <summary>
/// The CV's page inside the editor (F142): the page's own view, in the site's own look, drawn from the draft. What
/// is seen here is what a visitor gets once it is saved.
/// </summary>
public partial class ResumeSheet
{
    /// <summary>The draft to draw.</summary>
    [Parameter, EditorRequired]
    public Resume Resume { get; set; } = default!;
}
