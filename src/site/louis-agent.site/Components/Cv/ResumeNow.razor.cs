using LouisAgent.Site.Resumes;
using LouisAgent.Web.Components.Editing;
using LouisAgent.Web.Components.Editing.Inline;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Components.Cv;

/// <summary>
/// The CV's "now" band (F142): the project Louis is building as proof of work, drawn from the CV's own data (his pick
/// of three), so the admin's editor changes it and the guide reads the same words.
/// </summary>
public partial class ResumeNow
{
    /// <summary>A panel to fill in.</summary>
    private static readonly ResumeProjectPart Blank = new("", "");

    private int? _open;

    /// <summary>The project.</summary>
    [Parameter, EditorRequired]
    public ResumeProject Project { get; set; } = null!;

    /// <summary>Told the project with one thing changed. With nothing listening, the band is plain words.</summary>
    [Parameter]
    public EventCallback<ResumeProject> ProjectChanged { get; set; }

    /// <summary>Whether the band is being edited.</summary>
    private bool Editable => ProjectChanged.HasDelegate;

    /// <summary>The listener for the headline. With none, the band is headed by the project's name.</summary>
    private EventCallback<string?> HeadlineChanged =>
        ProjectChanged.Of<ResumeProject, string?>(this, headline => Project with { Headline = headline });

    /// <summary>The listener for the words under the headline.</summary>
    private EventCallback<string?> DescriptionChanged =>
        ProjectChanged.Of<ResumeProject, string?>(this, words => Project with { Description = words });

    /// <summary>The listener for a panel's name, which is never left out: an emptied one is blank.</summary>
    private EventCallback<string?> NameChanged(int at) =>
        Words(at, name => Project.Parts[at] with { Name = name ?? "" });

    /// <summary>The listener for a panel's text.</summary>
    private EventCallback<string?> SummaryChanged(int at) =>
        Words(at, text => Project.Parts[at] with { Summary = text ?? "" });

    /// <summary>A listener for some words of a panel, there exactly when someone listens for the project.</summary>
    private EventCallback<string?> Words(int at, Func<string?, ResumeProjectPart> changed) =>
        ProjectChanged.Of<ResumeProject, string?>(this, words => With(Project.Parts.Replaced(at, changed(words))));

    /// <summary>The project with these panels.</summary>
    private ResumeProject With(IReadOnlyList<ResumeProjectPart> parts) => Project with { Parts = parts };

    /// <summary>While the page is edited a panel's link leads nowhere, so a click doesn't leave the page.</summary>
    private string? AddressOf(string url) => Editable ? null : url;

    /// <summary>A panel's label words, with one added or taken out.</summary>
    private Task LabelWordsAsync(int at, IReadOnlyList<string> words) =>
        ProjectChanged.InvokeAsync(With(Project.Parts.Replaced(at, Project.Parts[at] with { Keywords = words })));

    /// <summary>Opens a panel's form, for its link.</summary>
    private void Open(int at) => _open = at;

    /// <summary>Closes the form, its edits dropped.</summary>
    private void Close() => _open = null;

    /// <summary>The open panel is kept with its edits, and its form closes.</summary>
    private Task KeepAsync(int at, ResumeProjectPart kept)
    {
        _open = null;
        return ProjectChanged.InvokeAsync(With(Project.Parts.Replaced(at, kept)));
    }

    /// <summary>A panel moves a step. An open form closes, since its place has changed.</summary>
    private Task MoveAsync(int at, int step)
    {
        _open = null;
        return ProjectChanged.InvokeAsync(With(Project.Parts.Moved(at, step)));
    }

    /// <summary>A panel is taken out.</summary>
    private Task RemoveAsync(int at)
    {
        _open = null;
        return ProjectChanged.InvokeAsync(With(Project.Parts.Without(at)));
    }

    /// <summary>One more panel, blank, at the end.</summary>
    private Task AddAsync() => ProjectChanged.InvokeAsync(With(Project.Parts.Added(Blank)));

    /// <summary>The words of a panel's link, in its form.</summary>
    private static Task LinkWordsAsync(EditedItem<ResumeProjectPart> part, string? words) =>
        part.Changed.InvokeAsync(part.Item with { Link = words });

    /// <summary>The address the link leads to.</summary>
    private static Task AddressAsync(EditedItem<ResumeProjectPart> part, string? address) =>
        part.Changed.InvokeAsync(part.Item with { Url = address });

    /// <summary>Whether a link leaves the site: a place on the site starts with a slash.</summary>
    private static bool Leaves(string url) => !url.StartsWith('/');

    /// <summary>A link that leaves the site opens a new tab.</summary>
    private static string? TargetOf(string url) => Leaves(url) ? "_blank" : null;

    /// <summary>A link that leaves the site tells the other site nothing about this one.</summary>
    private static string? RelOf(string url) => Leaves(url) ? "noopener" : null;
}
