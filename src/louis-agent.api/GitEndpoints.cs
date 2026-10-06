using System.Diagnostics;
using System.Text;
using louis_agent.core.tools;

namespace louis_agent.api;

/// <summary>
/// A small git interface for the web app: see what changed, read each diff, then stage, unstage, discard or commit.
/// git runs without a shell (arguments are passed as a list) and every path goes through the agent's workspace checks;
/// secret files (.env, keys) are listed but their diffs are never returned. Pushing is left to the user.
/// </summary>
internal static class GitEndpoints
{
    private const int MaxDiffChars = 500_000;
    private static readonly TimeSpan GitTimeout = TimeSpan.FromSeconds(30);

    public static void MapGit(this WebApplication app)
    {
        app.MapGet("/git/status", async (AgentEngine engine) => await StatusResultAsync(engine));

        app.MapGet("/git/diff", async (string? path, bool? staged, AgentEngine engine, WorkspaceTools workspace) =>
        {
            if (string.IsNullOrWhiteSpace(path))
                return ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error", "Pass the file's workspace-relative 'path'.");
            if (CheckPaths(workspace, [path]) is { } invalid) return invalid;
            if (WorkspaceTools.IsSensitive(path))
                return ApiErrors.Result(StatusCodes.Status403Forbidden, "permission_error", "Diffs of secret files (.env, keys, certificates) aren't shown.");

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

        app.MapPost("/git/stage", async (GitPathsRequest request, AgentEngine engine, WorkspaceTools workspace) =>
            await OnPathsAsync(request, engine, workspace, paths => RunAsync(engine, ["add", "--", .. paths])));

        app.MapPost("/git/unstage", async (GitPathsRequest request, AgentEngine engine, WorkspaceTools workspace) =>
            await OnPathsAsync(request, engine, workspace, paths => RunAsync(engine, ["restore", "--staged", "--", .. paths])));

        // Throws away working-tree changes: tracked files go back to their staged/committed content, untracked files are
        // deleted. Staged changes are kept (unstage first to drop those too). The web app asks for confirmation.
        app.MapPost("/git/discard", async (GitPathsRequest request, AgentEngine engine, WorkspaceTools workspace) =>
            await OnPathsAsync(request, engine, workspace, async paths =>
            {
                var status = ParseStatus((await RunAsync(engine, ["status", "--porcelain=v1", "-z", "--untracked-files=all"])).Output);
                var untracked = status.Where(f => f.Untracked).Select(f => f.Path).ToHashSet();
                foreach (string path in paths.Where(untracked.Contains))
                    File.Delete(workspace.ResolvePath(path));

                var tracked = paths.Where(p => !untracked.Contains(p)).ToList();
                return tracked.Count == 0 ? new GitResult(0, "", "") : await RunAsync(engine, ["restore", "--worktree", "--", .. tracked]);
            }));

        app.MapPost("/git/commit", async (GitCommitRequest request, AgentEngine engine) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error", "Write a commit message.");

            var commit = await RunAsync(engine, ["commit", "-m", request.Message.Trim()]);
            if (commit.ExitCode != 0)
            {
                string error = (commit.Error + commit.Output).Trim();
                if (error.Contains("Please tell me who you are", StringComparison.Ordinal) || error.Contains("empty ident", StringComparison.Ordinal))
                    error = "git has no author identity in the container: set GIT_AUTHOR_NAME, GIT_AUTHOR_EMAIL, GIT_COMMITTER_NAME and " +
                            "GIT_COMMITTER_EMAIL in config/.env.secrets and restart the api container.";
                else if (error.Contains("nothing to commit", StringComparison.Ordinal) || error.Contains("no changes added", StringComparison.Ordinal))
                    error = "Nothing is staged: stage the changes you want to commit first.";
                return ApiErrors.Result(StatusCodes.Status400BadRequest, "git_error", error);
            }

            var head = await RunAsync(engine, ["log", "-1", "--format=%h %s"]);
            return await StatusResultAsync(engine, committed: head.Output.Trim());
        });
    }

    private static async Task<IResult> OnPathsAsync(
        GitPathsRequest request, AgentEngine engine, WorkspaceTools workspace, Func<List<string>, Task<GitResult>> run)
    {
        var paths = (request.Paths ?? []).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
        if (paths.Count == 0)
            return ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error", "Pass at least one path in 'paths'.");
        if (CheckPaths(workspace, paths) is { } invalid) return invalid;

        var result = await run(paths);
        return result.ExitCode == 0 ? await StatusResultAsync(engine) : GitError(result);
    }

    /// <summary>Current branch and changed files; also the reply to every change, so the web app refreshes in one call.</summary>
    private static async Task<IResult> StatusResultAsync(AgentEngine engine, string? committed = null)
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

    /// <summary>Parses `git status --porcelain=v1 -z`: "XY path", with renames followed by their original path.</summary>
    internal static List<GitFileStatus> ParseStatus(string output)
    {
        var files = new List<GitFileStatus>();
        var entries = output.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < entries.Length; i++)
        {
            string entry = entries[i];
            if (entry.Length < 4) continue;
            char index = entry[0], workTree = entry[1];
            string path = entry[3..];
            string? oldPath = index is 'R' or 'C' && i + 1 < entries.Length ? entries[++i] : null;
            files.Add(new GitFileStatus(path, oldPath, index.ToString(), workTree.ToString()));
        }

        return files;
    }

    private static IResult? CheckPaths(WorkspaceTools workspace, IEnumerable<string> paths)
    {
        foreach (string path in paths)
        {
            try
            {
                workspace.ResolvePath(path);
            }
            catch (ArgumentException ex)
            {
                string message = ex.ParamName is null ? ex.Message : ex.Message.Replace($" (Parameter '{ex.ParamName}')", "");
                return ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error", $"{path}: {message}");
            }
        }

        return null;
    }

    private static IResult GitError(GitResult result) =>
        ApiErrors.Result(StatusCodes.Status400BadRequest, "git_error",
            string.IsNullOrWhiteSpace(result.Error) ? $"git exited with code {result.ExitCode}." : result.Error.Trim());

    // stdout and stderr are kept apart: git prints warnings (e.g. about line endings) on stderr that would corrupt
    // the porcelain and diff output.
    private static async Task<GitResult> RunAsync(AgentEngine engine, IEnumerable<string> args)
    {
        var psi = new ProcessStartInfo("git")
        {
            WorkingDirectory = engine.WorkspaceRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        // Report non-ASCII paths as-is instead of octal-escaped.
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add("core.quotepath=false");
        foreach (string arg in args) psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(GitTimeout);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            return new GitResult(-1, "", $"git timed out after {GitTimeout.TotalSeconds:0}s.");
        }

        return new GitResult(process.ExitCode, await output, await error);
    }

    private sealed record GitResult(int ExitCode, string Output, string Error);
}

/// <summary>One changed file. Index/WorkTree are git's X/Y status letters (" " = unchanged, "?" = untracked).</summary>
internal sealed record GitFileStatus(string Path, string? OldPath, string Index, string WorkTree)
{
    public bool Untracked => Index == "?";
    public bool Staged => Index is not (" " or "?");
    public bool Unstaged => Untracked || WorkTree != " ";
}

internal sealed record GitPathsRequest(List<string>? Paths);

internal sealed record GitCommitRequest(string? Message);
