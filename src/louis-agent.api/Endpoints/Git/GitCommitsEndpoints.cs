using LouisAgent.Api.Contracts.Git.Requests;
using LouisAgent.Api.Endpoints.Routing;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Messages;
using static LouisAgent.Api.Endpoints.Git.GitProcess;
using static LouisAgent.Api.Endpoints.Git.Output.GitOutputParser;

namespace LouisAgent.Api.Endpoints.Git;

/// <summary>
/// Creates a commit, reads the commit log across all branches and one commit's diff. git runs without a shell, and a
/// diff of a secret file is never returned.
/// </summary>
internal static class GitCommitsEndpoints
{
    public static void MapGitCommits(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/git/commits", async (GitCommitRequest request, AgentEngine engine) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error", MessageText.Format(ApiUserText.CommitMessageRequired));

            var commit = await RunAsync(engine, ["commit", "-m", request.Message.Trim()]);
            if (commit.ExitCode != 0)
            {
                string error = (commit.Error + commit.Output).Trim();
                if (error.Contains("Please tell me who you are", StringComparison.Ordinal) || error.Contains("empty ident", StringComparison.Ordinal))
                    error = MessageText.Format(ApiUserText.CommitNoIdentity);
                else if (error.Contains("nothing to commit", StringComparison.Ordinal) || error.Contains("no changes added", StringComparison.Ordinal))
                    error = MessageText.Format(ApiUserText.NothingStaged);
                return ApiErrors.Result(StatusCodes.Status400BadRequest, "git_error", error);
            }

            var head = await RunAsync(engine, ["log", "-1", "--format=%h %s"]);
            var sha = await RunAsync(engine, ["rev-parse", "HEAD"]);
            return await GitChangesEndpoints.StatusResultAsync(engine, committed: head.Output.Trim(),
                created: $"{ApiMap.Version}/git/commits/{sha.Output.Trim()}");
        });

        routes.MapGet("/git/commits", async (int? count, AgentEngine engine) =>
        {
            int limit = Math.Clamp(count ?? 200, 1, 1000);
            var log = await RunAsync(engine, ["log", "--all", "--date-order", "-n", limit.ToString(), $"--format={CommitFormat}{Record}"]);
            // A repository without commits has no log; that's an empty list, not an error.
            if (log.ExitCode != 0) return log.Error.Contains("does not have any commits") ? Results.Json(new { commits = Array.Empty<object>() }) : GitError(log);
            return Results.Json(new { commits = ParseCommits(log.Output) });
        });

        routes.MapGet("/git/commits/{sha}", async (string sha, AgentEngine engine) =>
        {
            if (!CommitHash().IsMatch(sha))
                return ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error", MessageText.Format(ApiUserText.ShaRequired));

            var meta = await RunAsync(engine, ["show", "-s", $"--format={CommitFormat}{Field}%B", sha]);
            if (meta.ExitCode != 0) return GitError(meta);
            string[] fields = meta.Output.Split(Field);
            var commit = ParseCommit(string.Join(Field, fields.Take(7)));
            string body = fields.Length > 7 ? fields[7].Trim() : "";

            // A merge commit shows what it brought in: its diff against the first parent.
            var patch = await RunAsync(engine, ["show", "--format=", "-M", "--diff-merges=first-parent", "--patch", sha]);
            if (patch.ExitCode != 0) return GitError(patch);
            var (diff, truncated) = SafeDiff(patch.Output);
            return Results.Json(new { commit, body, diff, truncated });
        });
    }
}
