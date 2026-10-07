namespace louis_agent.web.Services;

/// <summary>
/// Branches and the commit log across all branches, shared by the Branches panel and the branch/commit viewer.
/// Merging or deleting a branch refreshes both, and the working-tree status in <see cref="GitState"/>.
/// </summary>
/// <remarks>
/// Merges and deletes are queued, not refused: every click is kept and the actions run one at a time, in the order they
/// were clicked (git can't merge two branches into the same checkout at once). Only the branch an action is waiting
/// for is marked busy, so the rest of the panel stays usable while a merge runs.
/// </remarks>
public sealed class GitHistory(AgentApi api, GitState git)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, BranchAction> _pending = [];
    private bool _refreshing;

    public GitBranches? Branches { get; private set; }
    public List<GitCommit> Commits { get; private set; } = [];
    public string? Error { get; private set; }
    public string? Message { get; private set; }

    /// <summary>A refresh, merge or delete is running or queued.</summary>
    public bool Busy => _refreshing || _pending.Count > 0;

    public event Action? Changed;

    /// <summary>Branches with commits not yet in the checked-out branch.</summary>
    public int UnmergedCount => Branches?.Branches.Count(b => !b.Current && !b.Merged) ?? 0;

    public enum BranchAction { Merge, Delete }

    /// <summary>What the branch is waiting for, and whether it is the one running now (the rest are queued).</summary>
    public (BranchAction Action, bool Running)? Pending(string branch) =>
        _pending.TryGetValue(branch, out var action) ? (action, _running == branch) : null;

    /// <summary>Merges and deletes waiting behind the one that is running.</summary>
    public int QueuedCount => Math.Max(0, _pending.Count - (_running is null ? 0 : 1));

    private string? _running;

    public async Task RefreshAsync()
    {
        _refreshing = true;
        Changed?.Invoke();
        await _gate.WaitAsync();
        try
        {
            Branches = await api.GitBranchesAsync();
            Commits = (await api.GitLogAsync()).Commits;
            Error = null;
        }
        catch (Exception ex)
        {
            Error = Describe(ex);
        }
        finally
        {
            _refreshing = false;
            _gate.Release();
            Changed?.Invoke();
        }
    }

    public Task MergeAsync(string branch) => EnqueueAsync(BranchAction.Merge, branch);

    public Task DeleteAsync(string branch) => EnqueueAsync(BranchAction.Delete, branch);

    private async Task EnqueueAsync(BranchAction action, string branch)
    {
        // A second click on a branch that is already waiting changes nothing.
        if (!_pending.TryAdd(branch, action)) return;

        // A new batch of clicks starts with a clean slate; within a batch every failure is kept, so a conflict early in
        // the queue isn't hidden by the merges that succeed after it.
        if (_pending.Count == 1 && _running is null)
        {
            Message = null;
            Error = null;
        }
        Changed?.Invoke();

        await _gate.WaitAsync();
        _running = branch;
        Changed?.Invoke();
        try
        {
            Branches = await api.GitBranchActionAsync(action == BranchAction.Merge ? "merge" : "branch/delete", branch);
            Message = Branches.Message;
            Commits = (await api.GitLogAsync()).Commits;
        }
        catch (Exception ex)
        {
            Error = Error is null ? Describe(ex) : $"{Error}\n{Describe(ex)}";
        }
        finally
        {
            _pending.Remove(branch);
            _running = null;
            _gate.Release();
            Changed?.Invoke();
        }

        // The working tree changes with every merge; refresh it once the queue has drained.
        if (_pending.Count == 0) await git.RefreshAsync();
    }

    private static string Describe(Exception ex) =>
        ex is AgentApiException { Status: System.Net.HttpStatusCode.Unauthorized }
            ? "Enter the API key below to see branches."
            : ex.Message;
}
