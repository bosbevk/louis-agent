using LouisAgent.Site.Resumes.History;
using LouisAgent.Web.Components.Editing;
using LouisAgent.Web.Components.Editing.Inline;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Components.Cv.History;

/// <summary>
/// The CV's jobs (F142): a timeline, each job's dates beside what he did there and the stack he used. When someone
/// listens for a change (the admin's editor), a title and each point are changed where they stand, the stack is
/// tags, and the dates and the company, which the page draws as composed lines, open on a small form. The jobs are
/// a list, and so are a job's points. It never changes the jobs it was given. It hands back a new list.
/// </summary>
public partial class ResumeJobs
{
    /// <summary>A job to fill in.</summary>
    private static readonly ResumeWork Blank = new("", "", "");

    private int? _open;

    /// <summary>The jobs, newest first.</summary>
    [Parameter]
    public IReadOnlyList<ResumeWork> Work { get; set; } = [];

    /// <summary>Told the jobs with one thing changed. With nothing listening, they are plain words.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<ResumeWork>> WorkChanged { get; set; }

    /// <summary>Whether the jobs are being edited.</summary>
    private bool Editable => WorkChanged.HasDelegate;

    /// <summary>The company, with the few words about it after its name when there are some.</summary>
    private static string OrgOf(ResumeWork job) => job.Summary is { } summary ? $"{job.Name} · {summary}" : job.Name;

    /// <summary>The listener for a job's title, which is never left out: an emptied one is blank.</summary>
    private EventCallback<string?> TitleChanged(int at) =>
        Words(at, title => Work[at] with { Position = title ?? "" });

    /// <summary>The listener for a point under a job. A point emptied is a point taken out.</summary>
    private EventCallback<string?> PointChanged(int at, int point) => Words(at,
        words => Work[at] with
        {
            Highlights = words is null
                ? Work[at].Highlights.Without(point)
                : Work[at].Highlights.Replaced(point, words)
        });

    /// <summary>A listener for some words of a job, there exactly when someone listens for the jobs.</summary>
    private EventCallback<string?> Words(int at, Func<string?, ResumeWork> changed) =>
        WorkChanged.Of<IReadOnlyList<ResumeWork>, string?>(this, words => Work.Replaced(at, changed(words)));

    /// <summary>The jobs with this one changed in its place.</summary>
    private Task ChangeAsync(int at, ResumeWork job) => WorkChanged.InvokeAsync(Work.Replaced(at, job));

    /// <summary>A point moves a step among its job's points.</summary>
    private Task MovePointAsync(int at, int point, int step) =>
        ChangeAsync(at, Work[at] with { Highlights = Work[at].Highlights.Moved(point, step) });

    /// <summary>A point is taken out.</summary>
    private Task RemovePointAsync(int at, int point) =>
        ChangeAsync(at, Work[at] with { Highlights = Work[at].Highlights.Without(point) });

    /// <summary>One more point under a job, blank, to click and type.</summary>
    private Task AddPointAsync(int at) =>
        ChangeAsync(at, Work[at] with { Highlights = Work[at].Highlights.Added("") });

    /// <summary>A job's stack, with a tag added or taken out.</summary>
    private Task StackAsync(int at, IReadOnlyList<string> tags) => ChangeAsync(at, Work[at] with { Keywords = tags });

    /// <summary>Opens a job's form.</summary>
    private void Open(int at) => _open = at;

    /// <summary>Closes the form, its edits dropped.</summary>
    private void Close() => _open = null;

    /// <summary>The open job is kept with its edits, and its form closes.</summary>
    private Task KeepAsync(int at, ResumeWork kept)
    {
        _open = null;
        return ChangeAsync(at, kept);
    }

    /// <summary>A job moves a step. An open form closes, since its place has changed.</summary>
    private Task MoveAsync(int at, int step)
    {
        _open = null;
        return WorkChanged.InvokeAsync(Work.Moved(at, step));
    }

    /// <summary>A job is taken out.</summary>
    private Task RemoveAsync(int at)
    {
        _open = null;
        return WorkChanged.InvokeAsync(Work.Without(at));
    }

    /// <summary>One more job, blank, at the end, with its form open to fill it in.</summary>
    private Task AddAsync()
    {
        _open = Work.Count;
        return WorkChanged.InvokeAsync(Work.Added(Blank));
    }

    /// <summary>The company's name in a job's form.</summary>
    private static Task CompanyAsync(EditedItem<ResumeWork> job, string? company) =>
        job.Changed.InvokeAsync(job.Item with { Name = company ?? "" });

    /// <summary>The few words about the company.</summary>
    private static Task AboutAsync(EditedItem<ResumeWork> job, string? about) =>
        job.Changed.InvokeAsync(job.Item with { Summary = about });

    /// <summary>The month the job began, as 2021-03.</summary>
    private static Task FromAsync(EditedItem<ResumeWork> job, string? from) =>
        job.Changed.InvokeAsync(job.Item with { StartDate = from ?? "" });

    /// <summary>The month it ended. None while he still works there.</summary>
    private static Task ToAsync(EditedItem<ResumeWork> job, string? to) =>
        job.Changed.InvokeAsync(job.Item with { EndDate = to });
}
