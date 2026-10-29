using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Git.Output;
using LouisAgent.Api.Contracts.Git.Replies;
using LouisAgent.Api.Contracts.Git.Requests;
using LouisAgent.Api.Contracts.Workspace;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Messages;
using Microsoft.AspNetCore.Http.HttpResults;
using static LouisAgent.Api.Endpoints.Git.GitProcess;
using static LouisAgent.Api.Endpoints.Git.Output.GitOutputParser;
using BranchesAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Git.Replies.BranchesReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using BranchAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Git.Replies.BranchDiffReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints.Git.Branches;

/// <summary>
/// The Branches tab's routes as named methods: list the branches, show one branch's diff, merge one into the
/// checked-out branch, delete a merged one. Each checks first and stops with its error; nothing here pushes.
/// </summary>
/// <remarks>
/// A handler class per endpoint file (Introduce Parameter Object, Fowler): here the routes share no state of their
/// own, only the engine the framework hands each call, so the class holds nothing and each method takes the engine.
/// Each returns <c>TypedResults</c> with its answers declared (<c>BranchesAnswer</c>, <c>BranchAnswer</c>).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal sealed class GitBranchesHandlers
{
    /// <summary>Every branch, with how far each is ahead of and behind the checked-out one.</summary>
    public Task<BranchesAnswer> GetBranches(AgentEngine engine) => BranchesAsync(engine);

    /// <summary>One branch: what merging it would bring in, as its commits and its diff.</summary>
    public async Task<BranchAnswer> GetBranch(string name, AgentEngine engine)
    {
        var branches = await GitBranchList.ReadAsync(engine);
        if (branches.Error is { } error) return error;
        if (branches.Find(name) is not { } branch) return NoBranch(name);

        string reference = $"refs/heads/{branch.Name}";
        var log = await RunAsync(engine, ["log", $"--format={CommitFormat}{Record}", $"HEAD..{reference}"]);
        if (log.ExitCode != 0) return GitError(log);
        // Three dots: what the branch changed since it split off, which is what merging it would bring in.
        var patch = await RunAsync(engine, ["diff", "-M", $"HEAD...{reference}"]);
        if (patch.ExitCode != 0) return GitError(patch);

        var (diff, truncated) = SafeDiff(patch.Output);
        var reply = new BranchDiffReply(branch, branches.Current, ParseCommits(log.Output), diff, truncated);
        return TypedResults.Json(reply, WorkspaceJson.Web);
    }

    /// <summary>
    /// Merges a branch into the checked-out one with a merge commit; refused for the checked-out branch, a merged one,
    /// or a working tree with uncommitted changes. A merge that fails is undone and says why.
    /// </summary>
    public async Task<BranchesAnswer> Merge(GitBranchRequest request, AgentEngine engine)
    {
        var branches = await GitBranchList.ReadAsync(engine);
        if (branches.Error is { } error) return error;
        if (branches.Find(request.Branch) is not { } branch) return NoBranch(request.Branch);
        if (branch.Current)
            return Bad(MessageText.Format(ApiUserText.MergeCheckedOutBranch, new { Branch = branch.Name }));
        if (branch.Ahead == 0)
            return Bad(MessageText.Format(ApiUserText.MergeAlreadyMerged,
                new
                {
                    Branch = branch.Name,
                    branches.Current
                }));

        // A merge on top of uncommitted work would mix the two; untracked files are fine.
        var dirty = await RunAsync(engine, ["status", "--porcelain", "--untracked-files=no"]);
        if (dirty.Output.Trim().Length > 0)
            return ApiErrors.Conflict(MessageText.Format(ApiUserText.MergeUncommittedChanges,
                new { branches.Current }));

        string message = MessageText.Format(ApiUserText.MergeCommitMessage,
            new
            {
                Branch = branch.Name,
                branches.Current
            });
        var merge = await RunAsync(engine,
            ["merge", "--no-ff", "--no-edit", "-m", message, $"refs/heads/{branch.Name}"]);
        if (merge.ExitCode != 0) return await MergeFailedAsync(engine, branch, merge);

        var head = await RunAsync(engine, ["log", "-1", "--format=%h %s"]);
        return await BranchesAsync(engine, MessageText.Format(ApiUserText.Merged,
            new
            {
                Branch = branch.Name,
                branches.Current,
                Head = head.Output.Trim()
            }));
    }

    /// <summary>Deletes a branch that is merged and not checked out; git itself refuses unmerged work too.</summary>
    public async Task<BranchesAnswer> DeleteBranch(string name, AgentEngine engine)
    {
        var branches = await GitBranchList.ReadAsync(engine);
        if (branches.Error is { } error) return error;
        if (branches.Find(name) is not { } branch) return NoBranch(name);
        if (branch.Current) return Bad(MessageText.Format(ApiUserText.DeleteCurrentBranch));
        if (branch.Ahead > 0) return Bad(MessageText.Format(ApiUserText.BranchNotMerged, new { Branch = branch.Name }));

        var delete = await RunAsync(engine, ["branch", "-d", branch.Name]);
        if (delete.ExitCode != 0) return GitError(delete);
        return await BranchesAsync(engine,
            MessageText.Format(ApiUserText.BranchDeleted, new { Branch = branch.Name }));
    }

    /// <summary>The branch list, with a message saying what was just done, if anything.</summary>
    private static async Task<BranchesAnswer> BranchesAsync(AgentEngine engine, string? message = null)
    {
        var branches = await GitBranchList.ReadAsync(engine);
        if (branches.Error is { } error) return error;
        return TypedResults.Json(new BranchesReply(branches.Current, branches.Items, message), WorkspaceJson.Web);
    }

    /// <summary>
    /// A merge git refused: it is aborted, and the answer names the conflicting files, or else what git said (a missing
    /// name and email gets the words that say how to set them).
    /// </summary>
    private static async Task<JsonHttpResult<ApiError>> MergeFailedAsync(AgentEngine engine, GitBranchInfo branch,
        GitResult merge)
    {
        var conflicts = (await RunAsync(engine, ["diff", "--name-only", "--diff-filter=U"])).Output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        await RunAsync(engine, ["merge", "--abort"]);

        if (conflicts.Length > 0)
            return ApiErrors.Conflict(MessageText.Format(ApiUserText.MergeConflicts,
                new
                {
                    Branch = branch.Name,
                    Files = string.Join(", ", conflicts)
                }));
        string detail = (merge.Error + merge.Output).Trim();
        if (NoIdentity(detail)) detail = MessageText.Format(ApiUserText.MergeNoIdentity);
        return ApiErrors.Conflict(MessageText.Format(ApiUserText.MergeFailed,
            new
            {
                Branch = branch.Name,
                Detail = detail
            }));
    }

    /// <summary>git said it doesn't know who is committing.</summary>
    private static bool NoIdentity(string detail) =>
        detail.Contains("Please tell me who you are", StringComparison.Ordinal)
        || detail.Contains("empty ident", StringComparison.Ordinal);

    /// <summary>The 404 for a branch that doesn't exist.</summary>
    private static JsonHttpResult<ApiError> NoBranch(string? name) =>
        ApiErrors.NotFound(MessageText.Format(ApiUserText.NoBranch, new { Name = name }));

    /// <summary>A 400 with its reason.</summary>
    private static JsonHttpResult<ApiError> Bad(string message) => ApiErrors.BadRequest(message);
}
