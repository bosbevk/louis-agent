using LouisAgent.Site.Resumes;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Admin;

/// <summary>
/// The CV's page inside the editor (F142): the page's own view, in the site's own look, drawn from the draft and
/// edited where it stands. What is seen here is what a visitor gets once it is saved.
/// </summary>
public partial class ResumeSheet
{
    /// <summary>The draft to draw.</summary>
    [Parameter, EditorRequired]
    public Resume Resume { get; set; } = default!;

    /// <summary>
    /// Told the draft with one thing changed, each time something on the page is retyped, moved or taken out.
    /// </summary>
    [Parameter]
    public EventCallback<Resume> ResumeChanged { get; set; }
}
