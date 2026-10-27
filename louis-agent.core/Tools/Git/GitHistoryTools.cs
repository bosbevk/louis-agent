
using System.ComponentModel;
using System.Globalization;
using System.Text;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Git.Messages;
using static LouisAgent.Core.Tools.Git.Running.GitArguments;
using LouisAgent.Core.Tools.Git.Running;

namespace LouisAgent.Core.Tools.Git;
/// <summary>The git toolset's reading of history (S, CL15: one of its five classes): the log, a commit, the historian's tools (F22) and the repository overview. Every value from the model passes <see cref="GitArguments"/> (FX19) and runs on <see cref="GitRunner"/> (ADR 0004).</summary>
public sealed class GitHistoryTools(GitRunner git)
{
    private const string HistoryFormat = "--format=%h  %ad  %an  %s";

    [Description(
        "Return the most recent commits as a one-line-per-commit summary of hash and message (git log --oneline). Read-only, and does not show the full diff of each commit — use ShowCommit for that on a specific commit. The count parameter caps how many commits are returned (default 10); raise it for a longer history or lower it to save context.")]
    public string GetLog(
        [Description("Number of recent commits to show (default 10)")]
        int count = 10)
    {
        return git.Run(["log", "--oneline", "-n", count.ToString(CultureInfo.InvariantCulture)]);
    }

    [Description(
        "Show the full details of one commit — its message, author, and a per-file change summary, not a full diff (git show --stat) — for the given reference. Read-only. Use this to inspect a specific commit found via GetLog; use DiffStaged or DiffUnstaged instead if you want the current uncommitted changes rather than a historical commit.")]
    public string ShowCommit(
        [Description("Commit hash or reference (e.g., HEAD, HEAD~1)")]
        string commitRef)
    {
        return Refused(nameof(commitRef), commitRef) ?? git.Run(["show", "--stat", EndOfOptions, commitRef]);
    }

    [Description(
        "Return a combined summary of the repository — current branch, configured remotes, and working-tree status — by calling GetCurrentBranch, ListRemotes, and GetStatus together. Read-only. Use this for a quick overview instead of calling those three tools separately; call them individually if you only need one piece of information.")]
    public string GetRepositoryInfo()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Repository Info ===");
        sb.AppendLine($"Current Branch: {git.Run(["rev-parse", "--abbrev-ref", "HEAD"])}");
        sb.AppendLine($"Remote(s): {git.Run(["remote", "-v"])}");
        sb.AppendLine($"Status:\n{git.Run(["status", "--short"])}");
        return sb.ToString();
    }

    // ---- The historian's tools (F22): who wrote a line, a file's history, commits by text, author or date. Every value
    // from the model is its own argument (ProcessRunner's list), and "--" ends the options, so none can become a flag. ----

    [Description(
        "Who last changed each line of a file, with the commit and date (git blame). Give a line range for part of " +
        "a long file. Read-only.")]
    public string Blame(
        [Description("Workspace-relative file path.")]
        string relativePath,
        [Description("First line (default: the whole file)")]
        int startLine = 0,
        [Description("Last line (default: the whole file)")]
        int endLine = 0)
    {
        var args = new List<string> { "blame", "--date=short", "-w" };
        if (startLine > 0 && endLine >= startLine) args.AddRange(["-L", $"{startLine},{endLine}"]);
        args.AddRange(["--", relativePath]);
        return git.Run(args, GitToolMessage.GitNoCommits);
    }

    [Description(
        "The commits that changed one file, newest first, following renames: hash, date, author and message. " +
        "Read-only. Use ShowCommit for one commit's details.")]
    public string FileHistory(
        [Description("Workspace-relative file path.")]
        string relativePath,
        [Description("How many commits (1-100, default 20)")]
        int count = 20)
    {
        return git.Run([
            "log", "--follow", "-n", Math.Clamp(count, 1, 100).ToString(CultureInfo.InvariantCulture), "--date=short",
            HistoryFormat, "--", relativePath
        ], GitToolMessage.GitNoCommits);
    }

    [Description(
        "Find commits by words in their message, by author, or by date range (any combination), newest first: hash, " +
        "date, author and message. Dates like 2026-10-01 or '2 weeks ago'. Read-only.")]
    public string SearchCommits(
        [Description("Words in the commit message (case-insensitive), or empty")]
        string text = "",
        [Description("Author name or email (part of it), or empty")]
        string author = "",
        [Description("Only commits after this date, or empty")]
        string since = "",
        [Description("Only commits before this date, or empty")]
        string until = "",
        [Description("How many commits (1-100, default 20)")]
        int count = 20)
    {
        if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(author) && string.IsNullOrWhiteSpace(since) &&
            string.IsNullOrWhiteSpace(until))
            return MessageText.Format(GitToolMessage.GitSearchNeedsCriteria);

        var args = new List<string>
        {
            "log", "-i", "-n", Math.Clamp(count, 1, 100).ToString(CultureInfo.InvariantCulture), "--date=short",
            HistoryFormat
        };
        if (!string.IsNullOrWhiteSpace(text)) args.Add("--grep=" + text.Trim());
        if (!string.IsNullOrWhiteSpace(author)) args.Add("--author=" + author.Trim());
        if (!string.IsNullOrWhiteSpace(since)) args.Add("--since=" + since.Trim());
        if (!string.IsNullOrWhiteSpace(until)) args.Add("--until=" + until.Trim());
        return git.Run(args, GitToolMessage.GitNoCommits);
    }
}
