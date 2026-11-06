using LouisAgent.CodeReview.Workspace;
using LouisAgent.Web.Shared.Api;

namespace LouisAgent.CodeReview.Git;

/// <summary>
/// The workspace's git status, shared by the Changes panel, the diff viewer and the tab badge. Every change returns
/// the new status from the API, so one call both acts and refreshes.
/// </summary>
public sealed class GitState(AgentApi api)
{
    public GitStatus? Status { get; private set; }
    public string? Error { get; private set; }
    public bool Busy { get; private set; }

    public event Action? Changed;

    public int ChangeCount => Status?.Files.Count ?? 0;

    /// <summary>Fetches the status again.</summary>
    public Task RefreshAsync() => RunAsync(api.GitStatusAsync);

    /// <summary>Stages these files.</summary>
    public Task StageAsync(IEnumerable<string> paths) => RunAsync(() => api.GitChangeAsync("stage", paths.ToList()));

    /// <summary>Takes these files out of the staged group.</summary>
    public Task UnstageAsync(IEnumerable<string> paths) => RunAsync(() => api.GitChangeAsync("unstage", paths.ToList()));

    /// <summary>Throws away the changes to these files.</summary>
    public Task DiscardAsync(IEnumerable<string> paths) => RunAsync(() => api.GitChangeAsync("discard", paths.ToList()));

    /// <summary>Commits what's staged.</summary>
    public Task CommitAsync(string message) => RunAsync(() => api.GitCommitAsync(message));

    /// <summary>Marks us busy, makes the call and keeps the status it hands back, or says what went wrong.</summary>
    private async Task RunAsync(Func<Task<GitStatus>> call)
    {
        Busy = true;
        Changed?.Invoke();
        try
        {
            Status = await call();
            Error = null;
        }
        catch (Exception ex)
        {
            Error = ex is AgentApiException { Status: System.Net.HttpStatusCode.Unauthorized }
                ? "Enter the API key below to see changes."
                : ex.Message;
        }
        finally
        {
            Busy = false;
            Changed?.Invoke();
        }
    }
}
