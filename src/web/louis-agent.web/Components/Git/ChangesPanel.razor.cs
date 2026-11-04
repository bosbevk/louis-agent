using LouisAgent.Web.Git;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Git;

public partial class ChangesPanel : IDisposable
{
    [Parameter] public string? OpenKey { get; set; }
    [Parameter] public EventCallback<(GitFile File, bool Staged)> OnOpenDiff { get; set; }
    [Parameter] public EventCallback OnReviewAll { get; set; }

    private readonly HashSet<string> _selected = [];
    private string _message = "";
    private bool _confirmDiscard;

    public static string Key(GitFile file, bool staged) => $"{(staged ? "s" : "u")}:{file.Path}";

    private List<string> SelectedStaged => Selected(staged: true);
    private List<string> SelectedUnstaged => Selected(staged: false);

    // Only paths still present in that group count, so a refresh never acts on files that have moved on.
    private List<string> Selected(bool staged) =>
        (Git.Status?.Files ?? []).Where(f => (staged ? f.Staged : f.Unstaged) && _selected.Contains(Key(f, staged))).Select(f => f.Path).ToList();

    protected override async Task OnInitializedAsync()
    {
        Git.Changed += OnGitChanged;
        await Git.RefreshAsync();
    }

    private void OnGitChanged() => InvokeAsync(StateHasChanged);

    private async Task Stage()
    {
        var paths = SelectedUnstaged;
        await Git.StageAsync(paths);
        if (Git.Error is null) ForgetSelection(paths, staged: false);
    }

    private async Task Unstage()
    {
        var paths = SelectedStaged;
        await Git.UnstageAsync(paths);
        if (Git.Error is null) ForgetSelection(paths, staged: true);
    }

    private async Task Discard()
    {
        var paths = SelectedUnstaged;
        _confirmDiscard = false;
        await Git.DiscardAsync(paths);
        if (Git.Error is null) ForgetSelection(paths, staged: false);
    }

    private async Task Commit()
    {
        await Git.CommitAsync(_message);
        if (Git.Error is null)
        {
            _message = "";
            _selected.RemoveWhere(k => k.StartsWith("s:"));
        }
    }

    private void ForgetSelection(IEnumerable<string> paths, bool staged)
    {
        foreach (string path in paths) _selected.Remove($"{(staged ? "s" : "u")}:{path}");
    }

    private void Toggle(string key)
    {
        if (!_selected.Remove(key)) _selected.Add(key);
    }

    private void ToggleAll(List<GitFile> files, bool staged, bool select)
    {
        foreach (var file in files)
        {
            if (select) _selected.Add(Key(file, staged));
            else _selected.Remove(Key(file, staged));
        }
    }

    public void Dispose() => Git.Changed -= OnGitChanged;
}
