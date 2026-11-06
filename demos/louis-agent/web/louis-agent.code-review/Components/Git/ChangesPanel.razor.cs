using LouisAgent.CodeReview.Git;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.CodeReview.Components.Git;

/// <summary>The Changes tab: pick files, then stage, unstage, discard or commit them.</summary>
public partial class ChangesPanel : IDisposable
{
    [Parameter] public string? OpenKey { get; set; }
    [Parameter] public EventCallback<(GitFile File, bool Staged)> OnOpenDiff { get; set; }
    [Parameter] public EventCallback OnReviewAll { get; set; }

    private readonly HashSet<string> _selected = [];
    private string _message = "";
    private bool _confirmDiscard;

    /// <summary>A file's key in the selection: the same file can be picked as staged and as unstaged.</summary>
    public static string Key(GitFile file, bool staged) => $"{(staged ? "s" : "u")}:{file.Path}";

    private List<string> SelectedStaged => Selected(staged: true);
    private List<string> SelectedUnstaged => Selected(staged: false);

    // Only paths still present in that group count, so a refresh never acts on files that have moved on.
    private List<string> Selected(bool staged) =>
        (Git.Status?.Files ?? []).Where(f => (staged ? f.Staged : f.Unstaged) && _selected.Contains(Key(f, staged))).Select(f => f.Path).ToList();

    /// <summary>Listens for changes, then fetches the status.</summary>
    protected override async Task OnInitializedAsync()
    {
        Git.Changed += OnGitChanged;
        await Git.RefreshAsync();
    }

    /// <summary>Whatever changed the status, draw it again.</summary>
    private void OnGitChanged() => InvokeAsync(StateHasChanged);

    /// <summary>Stages what's picked, and forgets the picks once that worked.</summary>
    private async Task Stage()
    {
        var paths = SelectedUnstaged;
        await Git.StageAsync(paths);
        if (Git.Error is null) ForgetSelection(paths, staged: false);
    }

    /// <summary>Unstages what's picked, and forgets the picks once that worked.</summary>
    private async Task Unstage()
    {
        var paths = SelectedStaged;
        await Git.UnstageAsync(paths);
        if (Git.Error is null) ForgetSelection(paths, staged: true);
    }

    /// <summary>Throws away the picked changes, after the box asking to confirm.</summary>
    private async Task Discard()
    {
        var paths = SelectedUnstaged;
        _confirmDiscard = false;
        await Git.DiscardAsync(paths);
        if (Git.Error is null) ForgetSelection(paths, staged: false);
    }

    /// <summary>Commits what's staged. Once it worked, the message and the staged picks are cleared.</summary>
    private async Task Commit()
    {
        await Git.CommitAsync(_message);
        if (Git.Error is null)
        {
            _message = "";
            _selected.RemoveWhere(k => k.StartsWith("s:"));
        }
    }

    /// <summary>Drops these paths from the selection.</summary>
    private void ForgetSelection(IEnumerable<string> paths, bool staged)
    {
        foreach (string path in paths) _selected.Remove($"{(staged ? "s" : "u")}:{path}");
    }

    /// <summary>Picks a file, or unpicks it if it was picked.</summary>
    private void Toggle(string key)
    {
        if (!_selected.Remove(key)) _selected.Add(key);
    }

    /// <summary>The box at the top of a group: picks or unpicks every file in it.</summary>
    private void ToggleAll(List<GitFile> files, bool staged, bool select)
    {
        foreach (var file in files)
        {
            if (select) _selected.Add(Key(file, staged));
            else _selected.Remove(Key(file, staged));
        }
    }

    /// <summary>Stops listening when the panel goes.</summary>
    public void Dispose() => Git.Changed -= OnGitChanged;
}
