using Microsoft.AspNetCore.Components;

namespace LouisAgent.CodeReview.Components.Git;

/// <summary>The Branches tab: every branch and the commit log, from the shared history.</summary>
public partial class BranchesPanel : IDisposable
{
    [Parameter] public string? OpenKey { get; set; }
    [Parameter] public EventCallback<string> OnOpenBranch { get; set; }
    [Parameter] public EventCallback<string> OnOpenCommit { get; set; }

    /// <summary>The key the page uses to say this branch is the one open in the viewer.</summary>
    public static string BranchKey(string name) => $"b:{name}";

    /// <summary>The key the page uses to say this commit is the one open in the viewer.</summary>
    public static string CommitKey(string sha) => $"c:{sha}";

    /// <summary>Listens for changes, then fetches the branches and the log.</summary>
    protected override async Task OnInitializedAsync()
    {
        History.Changed += OnChanged;
        await History.RefreshAsync();
    }

    /// <summary>Whatever changed the history, draw it again.</summary>
    private void OnChanged() => InvokeAsync(StateHasChanged);

    /// <summary>Stops listening when the panel goes.</summary>
    public void Dispose() => History.Changed -= OnChanged;
}
