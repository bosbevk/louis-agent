namespace LouisAgent.Site.Resumes;

/// <summary>
/// One part of the project Louis is building now (F142), a panel on the page: its name, the skills it shows as small
/// label words, what it is, and a link to where the site shows it.
/// </summary>
/// <param name="Name">The part, such as "Evals in C#".</param>
/// <param name="Summary">What it is and what it proves.</param>
/// <param name="Keywords">The skills it shows, drawn as the panel's label line.</param>
/// <param name="Url">Where the site shows it; a place on the site starts with a slash.</param>
/// <param name="Link">What the link says, such as "How it works".</param>
public sealed record ResumeProjectPart(
    string Name,
    string Summary,
    IReadOnlyList<string>? Keywords = null,
    string? Url = null,
    string? Link = null)
{
    /// <summary>The label words, none when the file has none.</summary>
    public IReadOnlyList<string> Keywords { get; init; } = Keywords ?? [];
}
