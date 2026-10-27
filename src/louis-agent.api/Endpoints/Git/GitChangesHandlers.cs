using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Git.Replies;
using LouisAgent.Api.Contracts.Git.Requests;
using LouisAgent.Api.Contracts.Workspace;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Guardrails.Workspace;
using LouisAgent.Core.Messages;
using Microsoft.AspNetCore.Http.HttpResults;
using static LouisAgent.Api.Endpoints.Git.GitProcess;
using static LouisAgent.Api.Endpoints.Git.Output.GitOutputParser;
using StatusAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Git.Replies.StatusReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using DiffAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Git.Replies.DiffReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints.Git;

/// <summary>
/// The Changes tab's routes as named methods: see what changed, read a file's diff, then stage, unstage or discard.
/// Every path goes through the agent's workspace checks, and a secret file is listed but its diff never returned.
/// </summary>
/// <remarks>
/// A handler class per endpoint file (Introduce Parameter Object, Fowler): the routes share no state of their own,
/// only the engine and the workspace the framework hands each call, so the class holds nothing. Each returns
/// <c>TypedResults</c> with its answers declared (<c>StatusAnswer</c>, <c>DiffAnswer</c>).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal sealed class GitChangesHandlers
{
    /// <summary>The checked-out branch and every changed file.</summary>
    public async Task<StatusAnswer> GetStatus(AgentEngine engine) => Answer(await GitStatusRead.ReadAsync(engine));

    /// <summary>
    /// One file's diff, staged or not; an untracked file shows as wholly added. A secret file's diff is refused.
    /// </summary>
    public async Task<DiffAnswer> GetDiff(string? path, bool? staged, AgentEngine engine, WorkspaceBoundary workspace)
    {
        if (string.IsNullOrWhiteSpace(path)) return Bad(MessageText.Format(ApiUserText.PathRequired));
        if (CheckPaths(workspace, [path]) is { } invalid) return invalid;
        if (SensitiveFiles.IsSensitive(path))
            return ApiErrors.Result(StatusCodes.Status403Forbidden, "permission_error",
                MessageText.Format(ApiUserText.SecretDiffHidden));

        var status = await RunAsync(engine, ["status", "--porcelain=v1", "-z", "--untracked-files=all", "--", path]);
        bool untracked = status.Output.StartsWith("??", StringComparison.Ordinal);
        var diff = await DiffAsync(engine, path, staged == true, untracked);
        // git diff --no-index exits 1 when the files differ, which is the expected case here.
        if (diff.ExitCode != 0 && !(untracked && diff.ExitCode == 1)) return GitError(diff);

        string text = diff.Output;
        bool truncated = text.Length > MaxDiffChars;
        var reply = new DiffReply(path, staged == true, untracked, truncated ? text[..MaxDiffChars] : text, truncated);
        return TypedResults.Json(reply, WorkspaceJson.Web);
    }

    /// <summary>Stages the files.</summary>
    public Task<StatusAnswer> Stage(GitPathsRequest request, AgentEngine engine, WorkspaceBoundary workspace) =>
        OnPathsAsync(request, engine, workspace, paths => RunAsync(engine, ["add", "--", .. paths]));

    /// <summary>Unstages the files, keeping their changes in the working tree.</summary>
    public Task<StatusAnswer> Unstage(GitPathsRequest request, AgentEngine engine, WorkspaceBoundary workspace) =>
        OnPathsAsync(request, engine, workspace, paths => RunAsync(engine, ["restore", "--staged", "--", .. paths]));

    /// <summary>
    /// Throws away working-tree changes: tracked files go back to their staged or committed content, untracked files
    /// are deleted. Staged changes are kept (unstage first to drop those too). The web app asks for confirmation.
    /// </summary>
    public Task<StatusAnswer> Discard(GitPathsRequest request, AgentEngine engine, WorkspaceBoundary workspace) =>
        OnPathsAsync(request, engine, workspace, paths => DiscardAsync(engine, workspace, paths));

    /// <summary>The status as an answer, or the error git gave reading it.</summary>
    internal static StatusAnswer Answer(GitStatusRead read)
    {
        if (read.Error is { } error) return error;
        return TypedResults.Json(read.Reply!, WorkspaceJson.Web);
    }

    /// <summary>The diff of a file: against nothing for a new file, else the staged or the working-tree side.</summary>
    private static Task<GitResult> DiffAsync(AgentEngine engine, string path, bool staged, bool untracked)
    {
        // Untracked files have no history, so they are shown as entirely added.
        if (untracked) return RunAsync(engine, ["diff", "--no-index", "--", "/dev/null", path]);
        if (staged) return RunAsync(engine, ["diff", "--cached", "-M", "--", path]);
        return RunAsync(engine, ["diff", "-M", "--", path]);
    }

    /// <summary>Checks the paths, runs the change on them, and answers with the status after it.</summary>
    private static async Task<StatusAnswer> OnPathsAsync(GitPathsRequest request, AgentEngine engine,
        WorkspaceBoundary workspace, Func<List<string>, Task<GitResult>> run)
    {
        var paths = (request.Paths ?? []).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
        if (paths.Count == 0) return Bad(MessageText.Format(ApiUserText.PathsRequired));
        if (CheckPaths(workspace, paths) is { } invalid) return invalid;

        var result = await run(paths);
        if (result.ExitCode != 0) return GitError(result);
        return Answer(await GitStatusRead.ReadAsync(engine));
    }

    /// <summary>
    /// Deletes the untracked files named, and restores the tracked ones from what's staged or committed.
    /// </summary>
    private static async Task<GitResult> DiscardAsync(AgentEngine engine, WorkspaceBoundary workspace,
        List<string> paths)
    {
        var status = ParseStatus((await RunAsync(engine, ["status", "--porcelain=v1", "-z", "--untracked-files=all"]))
            .Output);
        var untracked = status.Where(f => f.Untracked).Select(f => f.Path).ToHashSet();
        foreach (string path in paths.Where(untracked.Contains))
            File.Delete(workspace.Resolve(path));

        var tracked = paths.Where(p => !untracked.Contains(p)).ToList();
        if (tracked.Count == 0) return new GitResult(0, "", "");
        return await RunAsync(engine, ["restore", "--worktree", "--", .. tracked]);
    }

    /// <summary>The 400 for the first path the workspace refuses, saying why; none when all are fine.</summary>
    private static JsonHttpResult<ApiError>? CheckPaths(WorkspaceBoundary workspace, IEnumerable<string> paths)
    {
        foreach (string path in paths)
        {
            try
            {
                workspace.Resolve(path);
            }
            catch (ArgumentException ex)
            {
                // The workspace's reason, without .NET's "(Parameter 'x')" tail.
                string message = ex.ParamName is null
                    ? ex.Message
                    : ex.Message.Replace($" (Parameter '{ex.ParamName}')", "");
                return Bad(MessageText.Format(ApiUserText.PathRefused,
                    new
                    {
                        Path = path,
                        Reason = message
                    }));
            }
        }

        return null;
    }

    /// <summary>A 400 with its reason.</summary>
    private static JsonHttpResult<ApiError> Bad(string message) =>
        ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error", message);
}
