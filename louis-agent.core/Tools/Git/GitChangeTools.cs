
using System.ComponentModel;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Git.Messages;
using static LouisAgent.Core.Tools.Git.Running.GitArguments;
using LouisAgent.Core.Tools.Git.Running;

namespace LouisAgent.Core.Tools.Git;
/// <summary>The git toolset's working tree and index (S, CL15: one of its five classes): status, staging, commits, diffs, discards, the stash. Every value from the model passes <see cref="GitArguments"/> (FX19) and runs on <see cref="GitRunner"/> (ADR 0004).</summary>
public sealed class GitChangeTools(GitRunner git)
{
    [Description(
        "Return the working tree's current state — staged, unstaged, and untracked files — in git's compact one-line-per-file format (git status --short). Read-only; it does not stage or discard anything. Call this before Stage, Commit, or DiscardUnstaged to see exactly what will be affected, and use HasUncommittedChanges instead if you only need a yes/no answer rather than the full file list.")]
    public string GetStatus()
    {
        return git.Run(["status", "--short"]);
    }

    [Description(
        "Check whether the working tree has any staged or unstaged changes (git status --porcelain), returning a plain yes/no-style message along with the changed files if any exist. Read-only. Use this instead of GetStatus when you only need to know whether there's anything uncommitted, not the full detailed status.")]
    public string HasUncommittedChanges()
    {
        string status = git.Run(["status", "--porcelain"], whenEmpty: GitToolMessage.NoUncommittedChanges);
        return status == MessageText.Format(GitToolMessage.NoUncommittedChanges)
            ? status
            : MessageText.Format(GitToolMessage.UncommittedChanges, new { Status = status });
    }

    [Description(
        "Add file changes to the index so they will be included in the next commit (git add). Pass a specific path to stage just that file, or the default '.' to stage every change in the workspace. This does not create a commit by itself — follow with Commit; use Unstage to reverse a staging mistake before committing.")]
    public string Stage(
        [Description("File path(s) to stage (use '.' for all changes)")]
        string pathSpec = ".")
    {
        return Refused(nameof(pathSpec), pathSpec) ?? git.Run(["add", "--", pathSpec]);
    }

    [Description(
        "Remove files from the index without discarding their changes (git reset). Pass a specific path to unstage just that file, or the default '.' to unstage everything currently staged. The file's actual content is untouched — the changes just move back to unstaged; use DiscardUnstaged instead if you want to throw the changes away entirely, not just unstage them.")]
    public string Unstage(
        [Description("File path(s) to unstage (use '.' for all staged changes)")]
        string pathSpec = ".")
    {
        return Refused(nameof(pathSpec), pathSpec) ?? git.Run(["reset", "--", pathSpec]);
    }

    [Description(
        "Create a commit from whatever is currently staged, using the given message. It does not stage anything itself — run Stage first, or the commit will fail or be empty if nothing is staged. Use AmendCommit instead if you want to fold new changes into the previous commit rather than creating a new one.")]
    public string Commit(
        [Description("Commit message")] string message)
    {
        return git.Run(["commit", "-m", message]);
    }

    [Description(
        "Rewrite the most recent commit to include whatever is currently staged (git commit --amend). By default (noEdit: true) the original commit message is kept unchanged; set noEdit: false to open the message for editing instead. This rewrites commit history — only use it on a commit that has not been pushed yet, or on a branch where force-pushing a rewritten history is expected.")]
    public string AmendCommit(
        [Description("If true, keep the previous commit message; if false, allow editing")]
        bool noEdit = true)
    {
        return git.Run(noEdit ? ["commit", "--amend", "--no-edit"] : ["commit", "--amend"]);
    }

    [Description(
        "Show the diff between the working tree and the index — changes that have not yet been staged (git diff). Pass a file path to scope the diff to one file, or leave it empty for the whole workspace. Use DiffStaged instead to see changes that are staged and about to be committed, not the unstaged ones.")]
    public string DiffUnstaged(
        [Description("Optional file path to show diff for a specific file")]
        string filePath = "")
    {
        if (string.IsNullOrWhiteSpace(filePath)) return git.Run(["diff"]);
        return Refused(nameof(filePath), filePath) ?? git.Run(["diff", "--", filePath]);
    }

    [Description(
        "Show the diff between the index and the last commit — changes that are staged and will be included in the next commit (git diff --cached). Pass a file path to scope the diff to one file, or leave it empty for everything staged. Use DiffUnstaged instead to see changes that have not been staged yet.")]
    public string DiffStaged(
        [Description("Optional file path to show diff for a specific file")]
        string filePath = "")
    {
        if (string.IsNullOrWhiteSpace(filePath)) return git.Run(["diff", "--cached"]);
        return Refused(nameof(filePath), filePath) ?? git.Run(["diff", "--cached", "--", filePath]);
    }

    [Description(
        "Permanently discard unstaged changes in the working directory, reverting the given files back to their last-committed state (git checkout --). This cannot be undone — the discarded edits are not recoverable afterward. Pass a specific path to discard just that file, or the default '.' to discard every unstaged change in the workspace; it does not touch staged changes, so run Unstage first if those need discarding too.")]
    public string DiscardUnstaged(
        [Description("File path(s) to discard (use '.' for all changes)")]
        string pathSpec = ".")
    {
        return Refused(nameof(pathSpec), pathSpec) ?? git.Run(["checkout", "--", pathSpec]);
    }

    [Description(
        "Reset the working directory and index to match a given commit or branch, discarding both staged and unstaged changes (git reset --hard). This is destructive and cannot be undone through this tool — uncommitted work matching the reset range is lost. Defaults to resetting to HEAD, discarding all local modifications while staying on the same commit; pass a different target to also move the branch pointer itself.")]
    public string ResetHard(
        [Description("Commit/branch to reset to (default HEAD)")]
        string target = "HEAD")
    {
        return Refused(nameof(target), target) ?? git.Run(["reset", "--hard", EndOfOptions, target]);
    }

    [Description(
        "Save all staged and unstaged changes to the stash and restore a clean working directory (git stash, or git stash push with a message when one is given). Use this to temporarily set aside in-progress work, for example before switching branches. Use ApplyStash to bring the changes back later; use Commit instead if you actually want to save the changes into history rather than set them aside.")]
    public string Stash(
        [Description("Optional description for the stash")]
        string message = "")
    {
        return string.IsNullOrWhiteSpace(message)
            ? git.Run(["stash"])
            : git.Run(["stash", "push", "-m", message]);
    }

    [Description(
        "List all stashed changes with their index and description (git stash list). Read-only. Use the stashRef values shown here, such as stash@{0}, as the argument to ApplyStash.")]
    public string ListStashes()
    {
        return git.Run(["stash", "list"]);
    }

    [Description(
        "Restore a previously stashed change into the working directory (git stash apply, or git stash pop when pop is true). With pop: false (default) the stash entry is kept in the stash list after applying, so it can be applied again later; with pop: true the entry is removed once applied. Defaults to the most recent stash (stash@{0}) — use ListStashes first to find the reference for an older one.")]
    public string ApplyStash(
        [Description("Stash reference (e.g., stash@{0}); if empty, applies most recent")]
        string stashRef = "stash@{0}",
        [Description("If true, pop (remove) the stash; if false, just apply")]
        bool pop = false)
    {
        return Refused(nameof(stashRef), stashRef) ?? git.Run(["stash", pop ? "pop" : "apply", EndOfOptions, stashRef]);
    }
}
