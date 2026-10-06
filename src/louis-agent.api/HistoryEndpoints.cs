using System.Text;
using System.Text.RegularExpressions;
using louis_agent.core.tools;
using static louis_agent.api.GitEndpoints;

namespace louis_agent.api;

/// <summary>
/// History for the web app's Branches tab: every branch with how far it is ahead of the checked-out branch, the commit
/// log across all branches, each commit's or branch's diff, and merging a branch into the checked-out one (or deleting a
/// merged branch). Branch names are only accepted if they exist, and diffs of secret files are never returned.
/// Nothing here pushes.
/// </summary>
internal static partial class HistoryEndpoints
{
    private const int MaxDiffChars = 500_000;
    private const char Field = '\x1f', Record = '\x1e';

    public static void MapHistory(this WebApplication app)
    {
        app.MapGet("/git/branches", async (AgentEngine engine) => await BranchesResultAsync(engine));

        app.MapGet("/git/log", async (int? count, AgentEngine engine) =>
        {
            int limit = Math.Clamp(count ?? 200, 1, 1000);
            var log = await RunAsync(engine, ["log", "--all", "--date-order", "-n", limit.ToString(), $"--format={CommitFormat}{Record}"]);
            // A repository without commits has no log; that's an empty list, not an error.
            if (log.ExitCode != 0) return log.Error.Contains("does not have any commits") ? Results.Json(new { commits = Array.Empty<object>() }) : GitError(log);
            return Results.Json(new { commits = ParseCommits(log.Output) });
        });

        app.MapGet("/git/commit", async (string? sha, AgentEngine engine) =>
        {
            if (sha is null || !CommitHash().IsMatch(sha))
                return ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error", "Pass the commit's 'sha' (7-40 hex characters).");

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

        app.MapGet("/git/branch", async (string? name, AgentEngine engine) =>
        {
            var branches = await ListBranchesAsync(engine);
            if (branches.Error is { } error) return error;
            if (branches.Items.FirstOrDefault(b => b.Name == name) is not { } branch)
                return ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", $"No branch named '{name}'.");

            string reference = $"refs/heads/{branch.Name}";
            var log = await RunAsync(engine, ["log", $"--format={CommitFormat}{Record}", $"HEAD..{reference}"]);
            if (log.ExitCode != 0) return GitError(log);
            // Three dots: what the branch changed since it split off, which is what merging it would bring in.
            var patch = await RunAsync(engine, ["diff", "-M", $"HEAD...{reference}"]);
            if (patch.ExitCode != 0) return GitError(patch);
            var (diff, truncated) = SafeDiff(patch.Output);
            return Results.Json(new { branch, into = branches.Current, commits = ParseCommits(log.Output), diff, truncated });
        });

        app.MapPost("/git/merge", async (GitBranchRequest request, AgentEngine engine) =>
        {
            var branches = await ListBranchesAsync(engine);
            if (branches.Error is { } error) return error;
            if (branches.Items.FirstOrDefault(b => b.Name == request.Branch) is not { } branch)
                return ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", $"No branch named '{request.Branch}'.");
            if (branch.Current)
                return ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error", $"'{branch.Name}' is the checked-out branch.");
            if (branch.Ahead == 0)
                return ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error", $"'{branch.Name}' is already merged into {branches.Current}.");

            // A merge on top of uncommitted work would mix the two; untracked files are fine.
            var dirty = await RunAsync(engine, ["status", "--porcelain", "--untracked-files=no"]);
            if (dirty.Output.Trim().Length > 0)
                return ApiErrors.Result(StatusCodes.Status409Conflict, "conflict_error",
                    $"{branches.Current} has uncommitted changes: commit, unstage or discard them in Changes before merging.");

            var merge = await RunAsync(engine, ["merge", "--no-ff", "--no-edit", "-m", $"Merge branch '{branch.Name}' into {branches.Current}", $"refs/heads/{branch.Name}"]);
            if (merge.ExitCode != 0)
            {
                var conflicts = (await RunAsync(engine, ["diff", "--name-only", "--diff-filter=U"])).Output
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                await RunAsync(engine, ["merge", "--abort"]);
                string detail = (merge.Error + merge.Output).Trim();
                if (detail.Contains("Please tell me who you are", StringComparison.Ordinal) || detail.Contains("empty ident", StringComparison.Ordinal))
                    detail = "git has no author identity: set GIT_AUTHOR_NAME, GIT_AUTHOR_EMAIL, GIT_COMMITTER_NAME and GIT_COMMITTER_EMAIL and restart the api.";
                return ApiErrors.Result(StatusCodes.Status409Conflict, "conflict_error", conflicts.Length > 0
                    ? $"Merging '{branch.Name}' conflicts in {string.Join(", ", conflicts)}; the merge was aborted, nothing changed."
                    : $"git could not merge '{branch.Name}': {detail}");
            }

            var head = await RunAsync(engine, ["log", "-1", "--format=%h %s"]);
            return await BranchesResultAsync(engine, message: $"Merged {branch.Name} into {branches.Current}: {head.Output.Trim()}");
        });

        // Only merged branches: git branch -d refuses to delete unmerged work.
        app.MapPost("/git/branch/delete", async (GitBranchRequest request, AgentEngine engine) =>
        {
            var branches = await ListBranchesAsync(engine);
            if (branches.Error is { } error) return error;
            if (branches.Items.FirstOrDefault(b => b.Name == request.Branch) is not { } branch)
                return ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", $"No branch named '{request.Branch}'.");
            if (branch.Current || branch.Ahead > 0)
                return ApiErrors.Result(StatusCodes.Status400BadRequest, "invalid_request_error",
                    branch.Current ? "Can't delete the checked-out branch." : $"'{branch.Name}' isn't merged yet; merge it first.");

            var delete = await RunAsync(engine, ["branch", "-d", branch.Name]);
            return delete.ExitCode == 0 ? await BranchesResultAsync(engine, message: $"Deleted {branch.Name}") : GitError(delete);
        });
    }

    // hash, short hash, author, author date, subject, refs, parents
    private const string CommitFormat = "%H%x1f%h%x1f%an%x1f%aI%x1f%s%x1f%D%x1f%P";

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

    internal static List<GitCommitInfo> ParseCommits(string output) =>
        output.Split(Record).Select(r => r.Trim('\n', '\r')).Where(r => r.Length > 0).Select(ParseCommit).ToList();

    private static GitCommitInfo ParseCommit(string record)
    {
        string[] f = record.Split(Field);
        string Get(int i) => i < f.Length ? f[i].Trim() : "";
        var refs = Get(5).Split(", ", StringSplitOptions.RemoveEmptyEntries).Select(r => r.Replace("HEAD -> ", "")).Where(r => r != "HEAD").ToList();
        var parents = Get(6).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        return new GitCommitInfo(Get(0), Get(1), Get(2), Get(3), Get(4), refs, parents);
    }

    /// <summary>Drops the content of secret files from a multi-file patch, and caps its size.</summary>
    internal static (string Diff, bool Truncated) SafeDiff(string patch)
    {
        var result = new StringBuilder();
        foreach (string section in Regex.Split(patch, "(?=^diff --git )", RegexOptions.Multiline))
        {
            var header = DiffHeader().Match(section);
            if (!header.Success || !WorkspaceTools.IsSensitive(header.Groups[1].Value))
            {
                result.Append(section);
                continue;
            }

            // Keep the "diff --git" line so the file is still listed, but none of its content.
            int end = section.IndexOf('\n');
            result.Append(end < 0 ? section + "\n" : section[..(end + 1)]).Append("Binary files differ (secret file: diff not shown)\n");
        }

        string text = result.ToString();
        return text.Length > MaxDiffChars ? (text[..MaxDiffChars], true) : (text, false);
    }

    [GeneratedRegex("^diff --git a/.+? b/(.+)$", RegexOptions.Multiline)]
    private static partial Regex DiffHeader();

    [GeneratedRegex("^[0-9a-fA-F]{7,40}$")]
    private static partial Regex CommitHash();
}

/// <summary>A local branch; Ahead/Behind count commits relative to the checked-out branch (Ahead 0 = merged).</summary>
internal sealed record GitBranchInfo(string Name, string Commit, string Subject, string Author, string Date, bool Current, int Ahead, int Behind)
{
    public bool Merged => !Current && Ahead == 0;
}

internal sealed record GitCommitInfo(string Sha, string ShortSha, string Author, string Date, string Subject, List<string> Refs, List<string> Parents);

internal sealed record GitBranchRequest(string? Branch);
