using LouisAgent.Api.Contracts.Git.Output;
using LouisAgent.Api.Contracts.Git.Requests;
using LouisAgent.Api.Endpoints.Git.Output;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Messages;
using static LouisAgent.Api.Endpoints.Git.GitProcess;
using static LouisAgent.Api.Endpoints.Git.Output.GitOutputParser;

namespace LouisAgent.Api.Endpoints.Git;

/// <summary>
/// The Branches tab: every branch with how far it is ahead of the checked-out branch, a branch's diff, merging a
/// branch into the checked-out one, and deleting a merged branch. Branch names are only accepted if they exist.
/// Nothing here pushes.
/// </summary>
internal static class GitBranchesEndpoints
{
    public static void MapGitBranches(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/git/branches", async (AgentEngine engine) => await BranchesResultAsync(engine));

        routes.MapGet("/git/branches/{**name}", async (string name, AgentEngine engine) =>
        {
            var branches = await ListBranchesAsync(engine);
            if (branches.Error is { } error) return error;
            if (branches.Items.FirstOrDefault(b => b.Name == name) is not { } branch)
                return ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", MessageText.Format(ApiUserText.NoBranch, new { Name = name }));

            string reference = $"refs/heads/{branch.Name}";
            var log = await RunAsync(engine, ["log", $"--format={CommitFormat}{Record}", $"HEAD..{reference}"]);
            if (log.ExitCode != 0) return GitError(log);
            // Three dots: what the branch changed since it split off, which is what merging it would bring in.
            var patch = await RunAsync(engine, ["diff", "-M", $"HEAD...{reference}"]);
            if (patch.ExitCode != 0) return GitError(patch);
            var (diff, truncated) = SafeDiff(patch.Output);
            return Results.Json(new { branch, into = branches.Current, commits = ParseCommits(log.Output), diff, truncated });
        });

        routes.MapPost("/git/merge", async (GitBranchRequest request, AgentEngine engine) =>
        {
            var branches = await ListBranchesAsync(engine);
            if (branches.Error is { } error) return error;
            if (branches.Items.FirstOrDefault(b => b.Name == request.Branch) is not { } branch)
                return ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", MessageText.Format(ApiUserText.NoBranch, new { Name = request.Branch }));
            if (branch.Current)
                return ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error", MessageText.Format(ApiUserText.MergeCheckedOutBranch, new { Branch = branch.Name }));
            if (branch.Ahead == 0)
                return ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error", MessageText.Format(ApiUserText.MergeAlreadyMerged, new { Branch = branch.Name, branches.Current }));

            // A merge on top of uncommitted work would mix the two; untracked files are fine.
            var dirty = await RunAsync(engine, ["status", "--porcelain", "--untracked-files=no"]);
            if (dirty.Output.Trim().Length > 0)
                return ApiErrors.Result(StatusCodes.Status409Conflict, "conflict_error",
                    MessageText.Format(ApiUserText.MergeUncommittedChanges, new { branches.Current }));

            var merge = await RunAsync(engine, ["merge", "--no-ff", "--no-edit", "-m", MessageText.Format(ApiUserText.MergeCommitMessage, new { Branch = branch.Name, branches.Current }), $"refs/heads/{branch.Name}"]);
            if (merge.ExitCode != 0)
            {
                var conflicts = (await RunAsync(engine, ["diff", "--name-only", "--diff-filter=U"])).Output
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                await RunAsync(engine, ["merge", "--abort"]);
                string detail = (merge.Error + merge.Output).Trim();
                if (detail.Contains("Please tell me who you are", StringComparison.Ordinal) || detail.Contains("empty ident", StringComparison.Ordinal))
                    detail = MessageText.Format(ApiUserText.MergeNoIdentity);
                return ApiErrors.Result(StatusCodes.Status409Conflict, "conflict_error", conflicts.Length > 0
                    ? MessageText.Format(ApiUserText.MergeConflicts, new { Branch = branch.Name, Files = string.Join(", ", conflicts) })
                    : MessageText.Format(ApiUserText.MergeFailed, new { Branch = branch.Name, Detail = detail }));
            }

            var head = await RunAsync(engine, ["log", "-1", "--format=%h %s"]);
            return await BranchesResultAsync(engine, message: MessageText.Format(ApiUserText.Merged, new { Branch = branch.Name, branches.Current, Head = head.Output.Trim() }));
        });

        // Only merged branches: git branch -d refuses to delete unmerged work.
        routes.MapDelete("/git/branches/{**name}", async (string name, AgentEngine engine) =>
        {
            var branches = await ListBranchesAsync(engine);
            if (branches.Error is { } error) return error;
            if (branches.Items.FirstOrDefault(b => b.Name == name) is not { } branch)
                return ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", MessageText.Format(ApiUserText.NoBranch, new { Name = name }));
            if (branch.Current || branch.Ahead > 0)
                return ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error",
                    branch.Current ? MessageText.Format(ApiUserText.DeleteCurrentBranch) : MessageText.Format(ApiUserText.BranchNotMerged, new { Branch = branch.Name }));

            var delete = await RunAsync(engine, ["branch", "-d", branch.Name]);
            return delete.ExitCode == 0 ? await BranchesResultAsync(engine, message: MessageText.Format(ApiUserText.BranchDeleted, new { Branch = branch.Name })) : GitError(delete);
        });
    }

    private static async Task<IResult> BranchesResultAsync(AgentEngine engine, string? message = null)
    {
        var branches = await ListBranchesAsync(engine);
        return branches.Error ?? Results.Json(new { current = branches.Current, branches = branches.Items, message });
    }

    private sealed record BranchList(string? Current, List<GitBranchInfo> Items, IResult? Error);

    private static async Task<BranchList> ListBranchesAsync(AgentEngine engine)
    {
        var head = await RunAsync(engine, ["rev-parse", "--abbrev-ref", "HEAD"]);
        string? current = head.ExitCode == 0 ? head.Output.Trim() : null;
        var refs = await RunAsync(engine, ["for-each-ref", "refs/heads", "--sort=-committerdate",
            "--format=%(refname:short)%1f%(objectname:short)%1f%(subject)%1f%(authorname)%1f%(committerdate:iso-strict)"]);
        if (refs.ExitCode != 0) return new BranchList(current, [], GitError(refs));

        var items = new List<GitBranchInfo>();
        foreach (string line in refs.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] f = line.TrimEnd('\r').Split(Field);
            if (f.Length < 5) continue;
            bool isCurrent = f[0] == current;
            int ahead = 0, behind = 0;
            if (!isCurrent && current is not null)
            {
                // "<behind>\t<ahead>": commits only on HEAD, then commits only on the branch.
                var counts = (await RunAsync(engine, ["rev-list", "--left-right", "--count", $"HEAD...refs/heads/{f[0]}"])).Output.Split('\t');
                if (counts.Length == 2)
                {
                    int.TryParse(counts[0], out behind);
                    int.TryParse(counts[1].Trim(), out ahead);
                }
            }
            items.Add(new GitBranchInfo(f[0], f[1], f[2], f[3], f[4], isCurrent, ahead, behind));
        }

        return new BranchList(current, items, null);
    }
}
