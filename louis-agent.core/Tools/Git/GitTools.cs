namespace LouisAgent.Core.Tools.Git;

using System.ComponentModel;
using System.Globalization;
using System.Text;
using LouisAgent.Core.Messages;

/// <summary>
/// The git toolset. Every call runs git from a list of arguments (<see cref="ProcessRunner"/>, ADR 0004), never a
/// command-line string, so a value from the model is one argument however it reads. And a list stops splitting, not
/// options (FX19): our own options come first, then <c>--end-of-options</c>, then the model's values, with paths after
/// <c>--</c>; and a value that names something (a ref, branch, tag, remote, URL, path or config key) and starts with
/// <c>-</c> is refused before git runs, so a command that takes neither marker is covered too. Free text that is an
/// option's own argument (a <c>-m</c> message, a config value) travels whole, whatever it starts with: git reads it as
/// that option's value. <c>SetConfig</c> sets only <c>user.name</c> and <c>user.email</c>: other keys make git run
/// commands.
/// </summary>
public sealed class GitTools
{
    private const string EndOfOptions = "--end-of-options";
    private static readonly string[] ConfigKeysAllowed = ["user.name", "user.email"];

    private readonly string _workspaceRoot;

    public GitTools(string workspaceRoot)
    {
        _workspaceRoot = workspaceRoot;
    }

    [Description("Return the name of the branch currently checked out (git rev-parse --abbrev-ref HEAD). Read-only — it does not change which branch is checked out. Use this to confirm which branch you are about to commit to or compare against before a push, merge, or branch-scoped operation; use ListBranches instead if you need to see all branches, not just the current one.")]
    public string GetCurrentBranch()
    {
        return RunGitList(["rev-parse", "--abbrev-ref", "HEAD"]);
    }

    [Description("Return the working tree's current state — staged, unstaged, and untracked files — in git's compact one-line-per-file format (git status --short). Read-only; it does not stage or discard anything. Call this before Stage, Commit, or DiscardUnstaged to see exactly what will be affected, and use HasUncommittedChanges instead if you only need a yes/no answer rather than the full file list.")]
    public string GetStatus()
    {
        return RunGitList(["status", "--short"]);
    }

    [Description("Return the most recent commits as a one-line-per-commit summary of hash and message (git log --oneline). Read-only, and does not show the full diff of each commit — use ShowCommit for that on a specific commit. The count parameter caps how many commits are returned (default 10); raise it for a longer history or lower it to save context.")]
    public string GetLog(
        [Description("Number of recent commits to show (default 10)")] int count = 10)
    {
        return RunGitList(["log", "--oneline", "-n", count.ToString(CultureInfo.InvariantCulture)]);
    }

    [Description("Show the full details of one commit — its message, author, and a per-file change summary, not a full diff (git show --stat) — for the given reference. Read-only. Use this to inspect a specific commit found via GetLog; use DiffStaged or DiffUnstaged instead if you want the current uncommitted changes rather than a historical commit.")]
    public string ShowCommit(
        [Description("Commit hash or reference (e.g., HEAD, HEAD~1)")] string commitRef)
    {
        return Refused(nameof(commitRef), commitRef) ?? RunGitList(["show", "--stat", EndOfOptions, commitRef]);
    }

    [Description("List branches in the repository (git branch). By default only local branches are shown; set all to true to also include remote-tracking branches (git branch -a). Read-only — it does not create, switch, or delete anything; use CreateBranch, SwitchBranch, or DeleteBranch for those.")]
    public string ListBranches(
        [Description("If true, show all branches (local + remote); if false, show only local")] bool all = false)
    {
        return RunGitList(all ? ["branch", "-a"] : ["branch"]);
    }

    [Description("Create a new branch starting from the current HEAD and switch to it in one step (git checkout -b). There is no option to create without switching. Fails if a branch with the given name already exists; use SwitchBranch instead if the branch already exists and you just want to move onto it.")]
    public string CreateBranch(
        [Description("Name for the new branch")] string branchName)
    {
        // -b's own argument: there is no --end-of-options to put it after, so the refusal alone guards it.
        return Refused(nameof(branchName), branchName) ?? RunGitList(["checkout", "-b", branchName]);
    }

    [Description("Check out an existing branch (git checkout), changing the working tree to match that branch's state. Uncommitted changes that conflict with the switch will block it rather than being silently discarded. Use CreateBranch instead if the branch does not exist yet.")]
    public string SwitchBranch(
        [Description("Name of the branch to switch to")] string branchName)
    {
        return Refused(nameof(branchName), branchName) ?? RunGitList(["checkout", EndOfOptions, branchName]);
    }

    [Description("Delete a local branch (git branch -d, or -D when force is true). The safe default (force: false) refuses to delete a branch with unmerged commits; set force: true only when you specifically intend to discard that branch's unmerged work, since it cannot be recovered afterward through this tool. This only deletes the local branch reference — it does not touch any remote branch of the same name.")]
    public string DeleteBranch(
        [Description("Name of the branch to delete")] string branchName,
        [Description("If true, force delete even if not merged")] bool force = false)
    {
        return Refused(nameof(branchName), branchName) ?? RunGitList(["branch", force ? "-D" : "-d", EndOfOptions, branchName]);
    }

    [Description("Add file changes to the index so they will be included in the next commit (git add). Pass a specific path to stage just that file, or the default '.' to stage every change in the workspace. This does not create a commit by itself — follow with Commit; use Unstage to reverse a staging mistake before committing.")]
    public string Stage(
        [Description("File path(s) to stage (use '.' for all changes)")] string pathSpec = ".")
    {
        return Refused(nameof(pathSpec), pathSpec) ?? RunGitList(["add", "--", pathSpec]);
    }

    [Description("Remove files from the index without discarding their changes (git reset). Pass a specific path to unstage just that file, or the default '.' to unstage everything currently staged. The file's actual content is untouched — the changes just move back to unstaged; use DiscardUnstaged instead if you want to throw the changes away entirely, not just unstage them.")]
    public string Unstage(
        [Description("File path(s) to unstage (use '.' for all staged changes)")] string pathSpec = ".")
    {
        return Refused(nameof(pathSpec), pathSpec) ?? RunGitList(["reset", "--", pathSpec]);
    }

    [Description("Create a commit from whatever is currently staged, using the given message. It does not stage anything itself — run Stage first, or the commit will fail or be empty if nothing is staged. Use AmendCommit instead if you want to fold new changes into the previous commit rather than creating a new one.")]
    public string Commit(
        [Description("Commit message")] string message)
    {
        return RunGitList(["commit", "-m", message]);
    }

    [Description("Rewrite the most recent commit to include whatever is currently staged (git commit --amend). By default (noEdit: true) the original commit message is kept unchanged; set noEdit: false to open the message for editing instead. This rewrites commit history — only use it on a commit that has not been pushed yet, or on a branch where force-pushing a rewritten history is expected.")]
    public string AmendCommit(
        [Description("If true, keep the previous commit message; if false, allow editing")] bool noEdit = true)
    {
        return RunGitList(noEdit ? ["commit", "--amend", "--no-edit"] : ["commit", "--amend"]);
    }

    [Description("Show the diff between the working tree and the index — changes that have not yet been staged (git diff). Pass a file path to scope the diff to one file, or leave it empty for the whole workspace. Use DiffStaged instead to see changes that are staged and about to be committed, not the unstaged ones.")]
    public string DiffUnstaged(
        [Description("Optional file path to show diff for a specific file")] string filePath = "")
    {
        if (string.IsNullOrWhiteSpace(filePath)) return RunGitList(["diff"]);
        return Refused(nameof(filePath), filePath) ?? RunGitList(["diff", "--", filePath]);
    }

    [Description("Show the diff between the index and the last commit — changes that are staged and will be included in the next commit (git diff --cached). Pass a file path to scope the diff to one file, or leave it empty for everything staged. Use DiffUnstaged instead to see changes that have not been staged yet.")]
    public string DiffStaged(
        [Description("Optional file path to show diff for a specific file")] string filePath = "")
    {
        if (string.IsNullOrWhiteSpace(filePath)) return RunGitList(["diff", "--cached"]);
        return Refused(nameof(filePath), filePath) ?? RunGitList(["diff", "--cached", "--", filePath]);
    }

    [Description("Permanently discard unstaged changes in the working directory, reverting the given files back to their last-committed state (git checkout --). This cannot be undone — the discarded edits are not recoverable afterward. Pass a specific path to discard just that file, or the default '.' to discard every unstaged change in the workspace; it does not touch staged changes, so run Unstage first if those need discarding too.")]
    public string DiscardUnstaged(
        [Description("File path(s) to discard (use '.' for all changes)")] string pathSpec = ".")
    {
        return Refused(nameof(pathSpec), pathSpec) ?? RunGitList(["checkout", "--", pathSpec]);
    }

    [Description("Reset the working directory and index to match a given commit or branch, discarding both staged and unstaged changes (git reset --hard). This is destructive and cannot be undone through this tool — uncommitted work matching the reset range is lost. Defaults to resetting to HEAD, discarding all local modifications while staying on the same commit; pass a different target to also move the branch pointer itself.")]
    public string ResetHard(
        [Description("Commit/branch to reset to (default HEAD)")] string target = "HEAD")
    {
        return Refused(nameof(target), target) ?? RunGitList(["reset", "--hard", EndOfOptions, target]);
    }

    [Description("Save all staged and unstaged changes to the stash and restore a clean working directory (git stash, or git stash push with a message when one is given). Use this to temporarily set aside in-progress work, for example before switching branches. Use ApplyStash to bring the changes back later; use Commit instead if you actually want to save the changes into history rather than set them aside.")]
    public string Stash(
        [Description("Optional description for the stash")] string message = "")
    {
        return string.IsNullOrWhiteSpace(message)
            ? RunGitList(["stash"])
            : RunGitList(["stash", "push", "-m", message]);
    }

    [Description("List all stashed changes with their index and description (git stash list). Read-only. Use the stashRef values shown here, such as stash@{0}, as the argument to ApplyStash.")]
    public string ListStashes()
    {
        return RunGitList(["stash", "list"]);
    }

    [Description("Restore a previously stashed change into the working directory (git stash apply, or git stash pop when pop is true). With pop: false (default) the stash entry is kept in the stash list after applying, so it can be applied again later; with pop: true the entry is removed once applied. Defaults to the most recent stash (stash@{0}) — use ListStashes first to find the reference for an older one.")]
    public string ApplyStash(
        [Description("Stash reference (e.g., stash@{0}); if empty, applies most recent")] string stashRef = "stash@{0}",
        [Description("If true, pop (remove) the stash; if false, just apply")] bool pop = false)
    {
        return Refused(nameof(stashRef), stashRef) ?? RunGitList(["stash", pop ? "pop" : "apply", EndOfOptions, stashRef]);
    }

    [Description("Push local commits on the current branch to a remote (git push). Defaults to the origin remote and the current branch if none are specified. The force option pushes with --force-with-lease rather than a bare --force, which refuses if the remote has commits you haven't seen, but it can still overwrite a teammate's pushed work on that branch — only use it when that's clearly intended, such as after an amend or rebase on a branch nobody else is building on.")]
    public string Push(
        [Description("Remote name (default 'origin')")] string remote = "origin",
        [Description("Branch name; if empty, pushes current branch")] string branch = "",
        [Description("If true, force push (dangerous!)")] bool force = false)
    {
        if ((Refused(nameof(remote), remote) ?? Refused(nameof(branch), branch)) is { } refused) return refused;
        var args = new List<string> { "push" };
        if (force) args.Add("--force-with-lease");
        args.AddRange([EndOfOptions, remote]);
        if (!string.IsNullOrWhiteSpace(branch)) args.Add(branch);
        return RunGitList(args);
    }

    [Description("Fetch and merge the latest commits from a remote branch into the current branch (git pull). Defaults to origin and the current branch's upstream if none are specified. This can create a merge commit or conflict if local and remote history have diverged — use Fetch instead if you want to see what's new on the remote without merging it in yet.")]
    public string Pull(
        [Description("Remote name (default 'origin')")] string remote = "origin",
        [Description("Branch name; if empty, pulls current branch")] string branch = "")
    {
        if ((Refused(nameof(remote), remote) ?? Refused(nameof(branch), branch)) is { } refused) return refused;
        var args = new List<string> { "pull", EndOfOptions, remote };
        if (!string.IsNullOrWhiteSpace(branch)) args.Add(branch);
        return RunGitList(args);
    }

    [Description("Download the latest commits and refs from a remote without merging them into the current branch (git fetch). This updates the repository's view of the remote without touching the working directory. Use Pull instead if you also want those changes merged into your current branch.")]
    public string Fetch(
        [Description("Remote name (default 'origin')")] string remote = "origin")
    {
        return Refused(nameof(remote), remote) ?? RunGitList(["fetch", EndOfOptions, remote]);
    }

    [Description("Merge the given branch or commit into the current branch (git merge), creating a merge commit if the histories have diverged, or fast-forwarding if possible. Conflicting changes will leave the merge unresolved and require manual conflict resolution — this tool does not resolve conflicts itself. Use Rebase instead if you want to replay the current branch's commits on top of another branch rather than merging them together.")]
    public string Merge(
        [Description("Branch name or commit to merge")] string source,
        [Description("Commit message for the merge; if empty, uses default")] string message = "")
    {
        if (Refused(nameof(source), source) is { } refused) return refused;
        return string.IsNullOrWhiteSpace(message)
            ? RunGitList(["merge", EndOfOptions, source])
            : RunGitList(["merge", "-m", message, EndOfOptions, source]);
    }

    [Description("Replay the current branch's commits on top of another branch or commit (git rebase), rewriting the current branch's history in the process. Set abort: true to cancel an in-progress rebase and return to the pre-rebase state instead of performing a new one — conflicts during a rebase will leave it in progress until resolved or aborted. Only rebase a branch that has not been pushed yet, or that nobody else is building on, since rebasing rewrites commits that may already be shared.")]
    public string Rebase(
        [Description("Branch or commit to rebase onto")] string onto,
        [Description("If true, abort an in-progress rebase")] bool abort = false)
    {
        if (abort) return RunGitList(["rebase", "--abort"]);
        return Refused(nameof(onto), onto) ?? RunGitList(["rebase", EndOfOptions, onto]);
    }

    [Description("Create a tag pointing at the current commit (git tag). Without a message this creates a lightweight tag with just a name; with a message it creates an annotated tag with name, message, and tagger metadata (git tag -a). This only creates the tag locally — it is not pushed to the remote by this tool.")]
    public string CreateTag(
        [Description("Tag name")] string tagName,
        [Description("Optional message for an annotated tag")] string message = "")
    {
        if (Refused(nameof(tagName), tagName) is { } refused) return refused;
        return string.IsNullOrWhiteSpace(message)
            ? RunGitList(["tag", EndOfOptions, tagName])
            : RunGitList(["tag", "-a", "-m", message, EndOfOptions, tagName]);
    }

    [Description("List all tags in the repository (git tag). Read-only.")]
    public string ListTags()
    {
        return RunGitList(["tag"]);
    }

    [Description("Delete a local tag by name (git tag -d). This only removes the tag locally — if it was already pushed to a remote, the remote copy is untouched and must be deleted separately.")]
    public string DeleteTag(
        [Description("Tag name to delete")] string tagName)
    {
        return Refused(nameof(tagName), tagName) ?? RunGitList(["tag", "-d", EndOfOptions, tagName]);
    }

    [Description("Return the URL configured for a remote (git remote get-url). Defaults to origin. Read-only; use AddRemote to add a new remote.")]
    public string GetRemoteUrl(
        [Description("Remote name (default 'origin')")] string remoteName = "origin")
    {
        return Refused(nameof(remoteName), remoteName) ?? RunGitList(["remote", "get-url", EndOfOptions, remoteName]);
    }

    [Description("List all configured remotes and their fetch/push URLs (git remote -v). Read-only.")]
    public string ListRemotes()
    {
        return RunGitList(["remote", "-v"]);
    }

    [Description("Add a new remote with the given name and URL (git remote add). Fails if a remote with that name already exists. Use GetRemoteUrl or ListRemotes first to check what's already configured.")]
    public string AddRemote(
        [Description("Name for the remote (e.g., 'origin')")] string name,
        [Description("URL for the remote")] string url)
    {
        return Refused(nameof(name), name) ?? Refused(nameof(url), url) ?? RunGitList(["remote", "add", EndOfOptions, name, url]);
    }

    [Description("Return a combined summary of the repository — current branch, configured remotes, and working-tree status — by calling GetCurrentBranch, ListRemotes, and GetStatus together. Read-only. Use this for a quick overview instead of calling those three tools separately; call them individually if you only need one piece of information.")]
    public string GetRepositoryInfo()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Repository Info ===");
        sb.AppendLine($"Current Branch: {GetCurrentBranch()}");
        sb.AppendLine($"Remote(s): {ListRemotes()}");
        sb.AppendLine($"Status:\n{GetStatus()}");
        return sb.ToString();
    }

    [Description("Apply the changes from a specific commit onto the current branch as a new commit (git cherry-pick). This can conflict if the target branch doesn't have the context the commit was originally made against — conflicts are left unresolved for manual handling, same as Merge and Rebase. Use this to bring in one specific commit from another branch without merging or rebasing the whole branch.")]
    public string CherryPick(
        [Description("Commit hash or reference to cherry-pick")] string commitRef)
    {
        return Refused(nameof(commitRef), commitRef) ?? RunGitList(["cherry-pick", EndOfOptions, commitRef]);
    }

    [Description("Check whether the working tree has any staged or unstaged changes (git status --porcelain), returning a plain yes/no-style message along with the changed files if any exist. Read-only. Use this instead of GetStatus when you only need to know whether there's anything uncommitted, not the full detailed status.")]
    public string HasUncommittedChanges()
    {
        string status = RunGitList(["status", "--porcelain"], whenEmpty: ToolMessage.NoUncommittedChanges);
        return status == MessageText.Format(ToolMessage.NoUncommittedChanges) ? status : MessageText.Format(ToolMessage.UncommittedChanges, new { Status = status });
    }

    [Description("Read git configuration values (git config). With no key, returns the full configuration list (git config --list); with a key such as user.name, returns just that value. Read-only; use SetConfig to change user.name or user.email.")]
    public string GetConfig(
        [Description("Config key (e.g., 'user.name'); if empty, shows all config")] string key = "")
    {
        if (string.IsNullOrWhiteSpace(key)) return RunGitList(["config", "--list"]);
        return Refused(nameof(key), key) ?? RunGitList(["config", "--get", EndOfOptions, key]);
    }

    [Description("Set user.name or user.email in the repository's local config (git config), so commits from this workspace carry that identity. Only those two keys: others (core.fsmonitor, core.sshCommand, core.hooksPath, alias.*) can make git run commands, so they are refused. Use GetConfig first to check the current value before overwriting it.")]
    public string SetConfig(
        [Description("Config key: user.name or user.email")] string key,
        [Description("Config value")] string value)
    {
        string name = key.Trim();
        if (!ConfigKeysAllowed.Contains(name, StringComparer.OrdinalIgnoreCase))
            return MessageText.Format(ToolMessage.GitConfigKeyRefused, new { Key = key, Allowed = string.Join(" and ", ConfigKeysAllowed) });
        return RunGitList(["config", EndOfOptions, name.ToLowerInvariant(), value]);
    }

    // ---- The historian's tools (F22): who wrote a line, a file's history, commits by text, author or date. Every value
    // from the model is its own argument (ProcessRunner's list), and "--" ends the options, so none can become a flag. ----

    private const string HistoryFormat = "--format=%h  %ad  %an  %s";

    [Description("Who last changed each line of a file, with the commit and date (git blame). Give a line range for part of " +
        "a long file. Read-only.")]
    public string Blame(
        [Description("Workspace-relative file path.")] string relativePath,
        [Description("First line (default: the whole file)")] int startLine = 0,
        [Description("Last line (default: the whole file)")] int endLine = 0)
    {
        var args = new List<string> { "blame", "--date=short", "-w" };
        if (startLine > 0 && endLine >= startLine) args.AddRange(["-L", $"{startLine},{endLine}"]);
        args.AddRange(["--", relativePath]);
        return RunGitList(args, ToolMessage.GitNoCommits);
    }

    [Description("The commits that changed one file, newest first, following renames: hash, date, author and message. " +
        "Read-only. Use ShowCommit for one commit's details.")]
    public string FileHistory(
        [Description("Workspace-relative file path.")] string relativePath,
        [Description("How many commits (1-100, default 20)")] int count = 20)
    {
        return RunGitList(["log", "--follow", "-n", Math.Clamp(count, 1, 100).ToString(CultureInfo.InvariantCulture), "--date=short",
            HistoryFormat, "--", relativePath], ToolMessage.GitNoCommits);
    }

    [Description("Find commits by words in their message, by author, or by date range (any combination), newest first: hash, " +
        "date, author and message. Dates like 2026-10-01 or '2 weeks ago'. Read-only.")]
    public string SearchCommits(
        [Description("Words in the commit message (case-insensitive), or empty")] string text = "",
        [Description("Author name or email (part of it), or empty")] string author = "",
        [Description("Only commits after this date, or empty")] string since = "",
        [Description("Only commits before this date, or empty")] string until = "",
        [Description("How many commits (1-100, default 20)")] int count = 20)
    {
        if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(author) && string.IsNullOrWhiteSpace(since) && string.IsNullOrWhiteSpace(until))
            return MessageText.Format(ToolMessage.GitSearchNeedsCriteria);

        var args = new List<string> { "log", "-i", "-n", Math.Clamp(count, 1, 100).ToString(CultureInfo.InvariantCulture), "--date=short", HistoryFormat };
        if (!string.IsNullOrWhiteSpace(text)) args.Add("--grep=" + text.Trim());
        if (!string.IsNullOrWhiteSpace(author)) args.Add("--author=" + author.Trim());
        if (!string.IsNullOrWhiteSpace(since)) args.Add("--since=" + since.Trim());
        if (!string.IsNullOrWhiteSpace(until)) args.Add("--until=" + until.Trim());
        return RunGitList(args, ToolMessage.GitNoCommits);
    }

    /// <summary>
    /// The refusal for a value that names something and starts with <c>-</c>: git would read it as an option, so it
    /// never runs. Null when the value is fine. Not for free text that is an option's own argument (<c>-m</c>).
    /// </summary>
    private static string? Refused(string parameter, string value) =>
        value.TrimStart().StartsWith('-')
            ? MessageText.Format(ToolMessage.GitValueRefused, new { Parameter = parameter, Value = value })
            : null;

    /// <summary>
    /// The one way git runs: each argument separate (no command line to split), in the workspace or the folder ACP set
    /// (<c>AGENT_WORKING_DIRECTORY</c>), 30 seconds at most. A non-zero exit carries git's own words.
    /// </summary>
    private string RunGitList(IReadOnlyList<string> args, ToolMessage whenEmpty = ToolMessage.SucceededNoOutput)
    {
        string workingDir = Environment.GetEnvironmentVariable("AGENT_WORKING_DIRECTORY") ?? _workspaceRoot;
        var result = ProcessRunner.Run("git", args, workingDir, TimeSpan.FromSeconds(30));
        if (result.TimedOut) return MessageText.Format(ToolMessage.GitTimeout);
        if (result.ExitCode != 0) return MessageText.Format(ToolMessage.ExitCode, new { Code = result.ExitCode, Output = result.Output.Trim() }).Trim();
        return string.IsNullOrWhiteSpace(result.Output) ? MessageText.Format(whenEmpty) : ProcessRunner.Truncate(result.Output.Trim());
    }
}
