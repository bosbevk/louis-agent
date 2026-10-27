using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Git.Replies;
using LouisAgent.Api.Contracts.Git.Requests;
using LouisAgent.Api.Contracts.Workspace;
using LouisAgent.Api.Endpoints.Git.Changes;
using LouisAgent.Api.Endpoints.Routing;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Messages;
using Microsoft.AspNetCore.Http.HttpResults;
using static LouisAgent.Api.Endpoints.Git.GitProcess;
using static LouisAgent.Api.Endpoints.Git.Output.GitOutputParser;
using CommittedAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.Created<LouisAgent.Api.Contracts.Git.Replies.StatusReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using CommitsAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Git.Replies.CommitsReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using CommitAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Git.Replies.CommitReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints.Git.Commits;

/// <summary>
/// The commit routes as named methods: commit what's staged, read the log across all branches, and read one commit
/// with its diff. git runs without a shell, and a secret file's diff is never returned.
/// </summary>
/// <remarks>
/// A handler class per endpoint file (Introduce Parameter Object, Fowler): the routes share no state of their own,
/// only the engine the framework hands each call, so the class holds nothing. Each returns <c>TypedResults</c> with its
/// answers declared. A new commit answers <c>Created</c>, which writes with the app's default JSON options; for this
/// wire those are <c>WorkspaceJson.Web</c>'s, so the reply reads the same.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal sealed class GitCommitsHandlers
{
    /// <summary>
    /// Commits what's staged: 201 with the status after it, naming the commit in Location (ADR 0039, rule 3); 400 for
    /// no message, nothing staged, or no name and email set.
    /// </summary>
    public async Task<CommittedAnswer> Commit(GitCommitRequest request, AgentEngine engine)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return Bad(MessageText.Format(ApiUserText.CommitMessageRequired));

        var commit = await RunAsync(engine, ["commit", "-m", request.Message.Trim()]);
        if (commit.ExitCode != 0)
            return ApiErrors.Result(StatusCodes.Status400BadRequest, "git_error", CommitRefused(commit));

        var head = await RunAsync(engine, ["log", "-1", "--format=%h %s"]);
        var sha = await RunAsync(engine, ["rev-parse", "HEAD"]);
        var read = await GitStatusRead.ReadAsync(engine, committed: head.Output.Trim());
        if (read.Error is { } error) return error;
        return TypedResults.Created($"{ApiMap.Version}/git/commits/{sha.Output.Trim()}", read.Reply);
    }

    /// <summary>The log across all branches, newest first, up to <paramref name="count"/> (200 by default).</summary>
    public async Task<CommitsAnswer> GetCommits(int? count, AgentEngine engine)
    {
        int limit = Math.Clamp(count ?? 200, 1, 1000);
        var log = await RunAsync(engine,
            ["log", "--all", "--date-order", "-n", limit.ToString(), $"--format={CommitFormat}{Record}"]);
        // A repository without commits has no log; that's an empty list, not an error.
        if (log.ExitCode != 0 && log.Error.Contains("does not have any commits"))
            return TypedResults.Json(new CommitsReply([]), WorkspaceJson.Web);
        if (log.ExitCode != 0) return GitError(log);
        return TypedResults.Json(new CommitsReply(ParseCommits(log.Output)), WorkspaceJson.Web);
    }

    /// <summary>One commit, with its whole message and its diff; a merge shows what it brought in.</summary>
    public async Task<CommitAnswer> GetCommit(string sha, AgentEngine engine)
    {
        if (!CommitHash().IsMatch(sha)) return Bad(MessageText.Format(ApiUserText.ShaRequired));

        var meta = await RunAsync(engine, ["show", "-s", $"--format={CommitFormat}{Field}%B", sha]);
        if (meta.ExitCode != 0) return GitError(meta);
        string[] fields = meta.Output.Split(Field);
        var commit = ParseCommit(string.Join(Field, fields.Take(7)));
        string body = fields.Length > 7 ? fields[7].Trim() : "";

        // A merge commit shows what it brought in: its diff against the first parent.
        var patch = await RunAsync(engine, ["show", "--format=", "-M", "--diff-merges=first-parent", "--patch", sha]);
        if (patch.ExitCode != 0) return GitError(patch);
        var (diff, truncated) = SafeDiff(patch.Output);
        return TypedResults.Json(new CommitReply(commit, body, diff, truncated), WorkspaceJson.Web);
    }

    /// <summary>
    /// Why git refused the commit, in the user's words for the two common cases: no name and email, nothing staged.
    /// </summary>
    private static string CommitRefused(GitResult commit)
    {
        string error = (commit.Error + commit.Output).Trim();
        if (error.Contains("Please tell me who you are", StringComparison.Ordinal)
            || error.Contains("empty ident", StringComparison.Ordinal))
            return MessageText.Format(ApiUserText.CommitNoIdentity);
        if (error.Contains("nothing to commit", StringComparison.Ordinal)
            || error.Contains("no changes added", StringComparison.Ordinal))
            return MessageText.Format(ApiUserText.NothingStaged);
        return error;
    }

    /// <summary>A 400 with its reason.</summary>
    private static JsonHttpResult<ApiError> Bad(string message) =>
        ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error", message);
}
