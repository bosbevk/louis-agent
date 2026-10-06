namespace louis_agent.web.Services;

/// <summary>
/// Branches and the commit log across all branches, shared by the Branches panel and the branch/commit viewer.
/// Merging or deleting a branch refreshes both, and the working-tree status in <see cref="GitState"/>.
/// </summary>
public sealed class GitHistory(AgentApi api, GitState git)
{
    public GitBranches? Branches { get; private set; }
    public List<GitCommit> Commits { get; private set; } = [];
    public string? Error { get; private set; }
    public string? Message { get; private set; }
    public bool Busy { get; private set; }

    public event Action? Changed;

    /// <summary>Branches with commits not yet in the checked-out branch.</summary>
    public int UnmergedCount => Branches?.Branches.Count(b => !b.Current && !b.Merged) ?? 0;

    public Task RefreshAsync() => RunAsync(async () =>
    {
        Branches = await api.GitBranchesAsync();
        Commits = (await api.GitLogAsync()).Commits;
    });

    public Task MergeAsync(string branch) => ChangeAsync("merge", branch);

    public Task DeleteAsync(string branch) => ChangeAsync("branch/delete", branch);

    private Task ChangeAsync(string action, string branch) => RunAsync(async () =>
    {
        Branches = await api.GitBranchActionAsync(action, branch);
        Message = Branches.Message;
        Commits = (await api.GitLogAsync()).Commits;
        await git.RefreshAsync();
    });

    private async Task RunAsync(Func<Task> call)
    {
        Busy = true;
        Message = null;
        Changed?.Invoke();
        try
        {
            await call();
            Error = null;
        }
        catch (Exception ex)
        {
            Error = ex is AgentApiException { Status: System.Net.HttpStatusCode.Unauthorized }
                ? "Enter the API key below to see branches."
                : ex.Message;
        }
        finally
        {
            Busy = false;
            Changed?.Invoke();
        }
    }
}
