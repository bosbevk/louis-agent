using System.Text;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools;
using LouisAgent.Orchestrator.Messages;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Orchestrator.Tools;

/// <summary>
/// The service's git repository, as the orchestrator checks it: back to main between fixes, and a fix commit verified
/// against what louis-agent said it did. Git always gets its arguments as a list, never through a shell (ADR 0004).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: a regular expression is the last option</seealso>
internal sealed class ServiceRepository(OrchestratorOptions options)
{
    private static readonly ILogger Log = AgentLogging.For<ServiceRepository>();
    private static readonly TimeSpan GitTimeout = TimeSpan.FromSeconds(30);

    /// <summary>The characters a branch name may hold, besides letters and digits.</summary>
    private const string BranchPunctuation = "._/-";

    /// <summary>
    /// A commit hash louis-agent reported: 7 to 40 hex characters. "abc1234" passes; "--all" or "HEAD~1" doesn't, so
    /// nothing the model writes can turn into a git option.
    /// </summary>
    public static bool IsCommitHash(string commit) =>
        commit.Length is >= 7 and <= 40 && commit.All(char.IsAsciiHexDigit);

    /// <summary>
    /// A branch name louis-agent reported: up to 100 letters, digits and <c>._/-</c>, not starting with a dash.
    /// "fix/order-total-abc123" passes; "-D" or "fix x" doesn't.
    /// </summary>
    public static bool IsBranchName(string branch) =>
        branch.Length is >= 1 and <= 100 && branch[0] != '-' &&
        branch.All(c => char.IsAsciiLetterOrDigit(c) || BranchPunctuation.Contains(c));

    /// <summary>
    /// Checks out main between fixes. Leftovers of an unfinished fix are stashed, not thrown away. Nothing when main is
    /// checked out, otherwise why it couldn't be.
    /// </summary>
    public string? ReturnToMain()
    {
        if (StashLeftovers() is { } problem) return problem;
        if (Head() == options.MainBranch) return null;

        var main = Git("switch", options.MainBranch);
        if (main.ExitCode == 0) return null;
        return MessageText.Format(OrchestratorToolMessage.CheckoutMainFailed,
            new { Branch = options.MainBranch, Output = main.Output.Trim() });
    }

    /// <summary>
    /// Checks a fix commit against what louis-agent said: it exists, sits on the branch, which is checked out, starts
    /// at main's tip, isn't on main, changed files, and left the tree clean. The report lists each check, then
    /// VERIFIED or NOT VERIFIED.
    /// </summary>
    public (string Report, bool Ok) Verify(string commit, string branch)
    {
        var report = new StringBuilder();
        bool ok = true;

        void Check(OrchestratorToolMessage name, object? values, bool passed, string detail = "")
        {
            ok &= passed;
            string suffix = detail.Length > 0 ? $": {detail}" : "";
            report.AppendLine($"{(passed ? "PASS" : "FAIL")} {MessageText.Format(name, values)}{suffix}");
        }

        // Nothing else is worth checking for a commit that doesn't exist.
        string type = Git("cat-file", "-t", commit).Output.Trim();
        Check(OrchestratorToolMessage.CheckCommitExists, null, type == "commit", type);
        if (!ok) return Finish();

        string head = Head();
        string main = options.MainBranch;
        Check(OrchestratorToolMessage.CheckBranchCheckedOut, null, head == branch,
            MessageText.Format(OrchestratorToolMessage.CheckHeadIs, new { Branch = head }));
        Check(OrchestratorToolMessage.CheckCommitOnBranch, new { Branch = branch },
            Git("merge-base", "--is-ancestor", commit, branch).ExitCode == 0);
        Check(OrchestratorToolMessage.CheckCommitNotOnMain, new { Branch = main },
            Git("merge-base", "--is-ancestor", commit, main).ExitCode == 1);
        Check(OrchestratorToolMessage.CheckBranchFromMainTip, new { Branch = main },
            Git("merge-base", main, branch).Output.Trim() == Git("rev-parse", main).Output.Trim());

        string files = Git("show", "--name-only", "--format=", commit).Output.Trim();
        Check(OrchestratorToolMessage.CheckCommitChangesFiles, null, files.Length > 0, files.ReplaceLineEndings(", "));
        string status = Git("status", "--porcelain").Output.Trim();
        Check(OrchestratorToolMessage.CheckWorkingTreeClean, null, status.Length == 0, status.ReplaceLineEndings(", "));

        report.AppendLine(Git("show", "--no-patch", "--format=author: %an <%ae>%nmessage: %s", commit).Output.Trim());
        return Finish();

        (string, bool) Finish() => (report.AppendLine(ok ? "VERIFIED" : "NOT VERIFIED").ToString(), ok);
    }

    /// <summary>Stashes a fix's leftovers, if any; nothing when that worked, otherwise why it didn't.</summary>
    private string? StashLeftovers()
    {
        if (Git("status", "--porcelain").Output.Trim().Length == 0) return null;

        string head = Head();
        var stash = Git("stash", "push", "--include-untracked", "-m",
            MessageText.Format(OrchestratorToolMessage.StashMessage, new { Branch = head }));
        Log.Log(LogLevel.Information, OrchestratorLogMessage.Stashed, new { Branch = head }, tag: "orchestrator");
        if (stash.ExitCode == 0) return null;
        return MessageText.Format(OrchestratorToolMessage.StashFailed,
            new { Branch = head, Output = stash.Output.Trim() });
    }

    /// <summary>The branch that's checked out.</summary>
    private string Head() => Git("rev-parse", "--abbrev-ref", "HEAD").Output.Trim();

    /// <summary>Runs git in the service's repository.</summary>
    private ProcessRunner.Result Git(params string[] args) =>
        ProcessRunner.Run("git", args, options.ServiceRoot, GitTimeout);
}
