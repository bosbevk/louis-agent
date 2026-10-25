using LouisAgent.Api.Endpoints.Git.Requests;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Guardrails.Workspace;
using LouisAgent.Core.Messages;
using static LouisAgent.Api.Endpoints.Git.GitProcess;
using static LouisAgent.Api.Endpoints.Git.Output.GitOutputParser;

namespace LouisAgent.Api.Endpoints.Git;

/// <summary>
/// A small git interface for the web app: see what changed, read each diff, then stage, unstage or discard.
/// git runs without a shell (arguments are passed as a list) and every path goes through the agent's workspace checks;
/// secret files (.env, keys) are listed but their diffs are never returned. Pushing is left to the user.
/// </summary>
internal static class GitChangesEndpoints
{
    public static void MapGitChanges(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/git/status", async (AgentEngine engine) => await StatusResultAsync(engine));

        routes.MapGet("/git/diff", async (string? path, bool? staged, AgentEngine engine, WorkspaceBoundary workspace) =>
        {
            if (string.IsNullOrWhiteSpace(path))
                return ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error", MessageText.Format(ApiUserText.PathRequired));
            if (CheckPaths(workspace, [path]) is { } invalid) return invalid;
            if (SensitiveFiles.IsSensitive(path))
                return ApiErrors.Result(StatusCodes.Status403Forbidden, "permission_error", MessageText.Format(ApiUserText.SecretDiffHidden));

            var status = await RunAsync(engine, ["status", "--porcelain=v1", "-z", "--untracked-files=all", "--", path]);
            bool untracked = status.Output.StartsWith("??", StringComparison.Ordinal);

            // Untracked files have no history, so they are shown as entirely added.
            GitResult diff = untracked
                ? await RunAsync(engine, ["diff", "--no-index", "--", "/dev/null", path])
                : await RunAsync(engine, staged == true ? ["diff", "--cached", "-M", "--", path] : ["diff", "-M", "--", path]);
            // git diff --no-index exits 1 when the files differ, which is the expected case here.
            if (diff.ExitCode != 0 && !(untracked && diff.ExitCode == 1)) return GitError(diff);

            string text = diff.Output;
            bool truncated = text.Length > MaxDiffChars;
            return Results.Json(new { path, staged = staged == true, untracked, diff = truncated ? text[..MaxDiffChars] : text, truncated });
        });

        routes.MapPost("/git/stage", async (GitPathsRequest request, AgentEngine engine, WorkspaceBoundary workspace) =>
            await OnPathsAsync(request, engine, workspace, paths => RunAsync(engine, ["add", "--", .. paths])));

        routes.MapPost("/git/unstage", async (GitPathsRequest request, AgentEngine engine, WorkspaceBoundary workspace) =>
            await OnPathsAsync(request, engine, workspace, paths => RunAsync(engine, ["restore", "--staged", "--", .. paths])));

        // Throws away working-tree changes: tracked files go back to their staged/committed content, untracked files are
        // deleted. Staged changes are kept (unstage first to drop those too). The web app asks for confirmation.
        routes.MapPost("/git/discard", async (GitPathsRequest request, AgentEngine engine, WorkspaceBoundary workspace) =>
            await OnPathsAsync(request, engine, workspace, async paths =>
            {
                var status = ParseStatus((await RunAsync(engine, ["status", "--porcelain=v1", "-z", "--untracked-files=all"])).Output);
                var untracked = status.Where(f => f.Untracked).Select(f => f.Path).ToHashSet();
                foreach (string path in paths.Where(untracked.Contains))
                    File.Delete(workspace.Resolve(path));

                var tracked = paths.Where(p => !untracked.Contains(p)).ToList();
                return tracked.Count == 0 ? new GitResult(0, "", "") : await RunAsync(engine, ["restore", "--worktree", "--", .. tracked]);
            }));
    }

    private static async Task<IResult> OnPathsAsync(
        GitPathsRequest request, AgentEngine engine, WorkspaceBoundary workspace, Func<List<string>, Task<GitResult>> run)
    {
        var paths = (request.Paths ?? []).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
        if (paths.Count == 0)
            return ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error", MessageText.Format(ApiUserText.PathsRequired));
        if (CheckPaths(workspace, paths) is { } invalid) return invalid;

        var result = await run(paths);
        return result.ExitCode == 0 ? await StatusResultAsync(engine) : GitError(result);
    }

    /// <summary>Current branch and changed files; also the reply to every change, so the web app refreshes in one call.</summary>
    internal static async Task<IResult> StatusResultAsync(AgentEngine engine, string? committed = null)
    {
        var status = await RunAsync(engine, ["status", "--porcelain=v1", "-z", "--untracked-files=all"]);
        if (status.ExitCode != 0) return GitError(status);
        var branch = await RunAsync(engine, ["rev-parse", "--abbrev-ref", "HEAD"]);

        return Results.Json(new
        {
            branch = branch.ExitCode == 0 ? branch.Output.Trim() : null,
            files = ParseStatus(status.Output),
            committed,
        });
    }

    private static IResult? CheckPaths(WorkspaceBoundary workspace, IEnumerable<string> paths)
    {
        foreach (string path in paths)
        {
            try
            {
                workspace.Resolve(path);
            }
            catch (ArgumentException ex)
            {
                string message = ex.ParamName is null ? ex.Message : ex.Message.Replace($" (Parameter '{ex.ParamName}')", "");
                return ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error", $"{path}: {message}");
            }
        }

        return null;
    }
}
