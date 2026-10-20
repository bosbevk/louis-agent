using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Git;

public partial class BranchesPanel : IDisposable
{
    [Parameter] public string? OpenKey { get; set; }
    [Parameter] public EventCallback<string> OnOpenBranch { get; set; }
    [Parameter] public EventCallback<string> OnOpenCommit { get; set; }

    public static string BranchKey(string name) => $"b:{name}";
    public static string CommitKey(string sha) => $"c:{sha}";

    protected override async Task OnInitializedAsync()
    {
        History.Changed += OnChanged;
        await History.RefreshAsync();
    }

    private void OnChanged() => InvokeAsync(StateHasChanged);

    public void Dispose() => History.Changed -= OnChanged;
}
