using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Code;

/// <summary>
/// Code on a page (F160.1): a listing named for its file, or its language when it has none, the code shown as text so
/// a page that shows a program can never run it or break with it.
/// </summary>
public partial class CodeListing
{
    /// <summary>The code, shown exactly as given.</summary>
    [Parameter, EditorRequired]
    public string Code { get; set; } = "";

    /// <summary>Its language, as a highlighter names it: <c>csharp</c>, <c>bash</c>, <c>json</c>.</summary>
    [Parameter, EditorRequired]
    public string Language { get; set; } = "";

    /// <summary>Its file's name, such as <c>Program.cs</c>, when it has one.</summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>What the listing is named: its file, else its language.</summary>
    private string Label => Name ?? Language;
}
