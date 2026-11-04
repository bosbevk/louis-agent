namespace LouisAgent.Core.Tools.Git;

using System.ComponentModel;
using LouisAgent.Core.Tools.Git.Running;
using static LouisAgent.Core.Tools.Git.Running.GitArguments;

/// <summary>The git toolset's branches and tags, and what joins them (S, CL15: one of its five classes): merge, rebase, cherry-pick. Every value from the model passes <see cref="GitArguments"/> (FX19) and runs on <see cref="GitRunner"/> (ADR 0004).</summary>
public sealed class GitBranchTools(GitRunner git)
{
    [Description("Return the name of the branch currently checked out (git rev-parse --abbrev-ref HEAD). Read-only — it does not change which branch is checked out. Use this to confirm which branch you are about to commit to or compare against before a push, merge, or branch-scoped operation; use ListBranches instead if you need to see all branches, not just the current one.")]
    public string GetCurrentBranch()
    {
        return git.Run(["rev-parse", "--abbrev-ref", "HEAD"]);
    }

    [Description("List branches in the repository (git branch). By default only local branches are shown; set all to true to also include remote-tracking branches (git branch -a). Read-only — it does not create, switch, or delete anything; use CreateBranch, SwitchBranch, or DeleteBranch for those.")]
    public string ListBranches(
        [Description("If true, show all branches (local + remote); if false, show only local")] bool all = false)
    {
        return git.Run(all ? ["branch", "-a"] : ["branch"]);
    }

    [Description("Create a new branch starting from the current HEAD and switch to it in one step (git checkout -b). There is no option to create without switching. Fails if a branch with the given name already exists; use SwitchBranch instead if the branch already exists and you just want to move onto it.")]
    public string CreateBranch(
        [Description("Name for the new branch")] string branchName)
    {
        // -b's own argument: there is no --end-of-options to put it after, so the refusal alone guards it.
        return Refused(nameof(branchName), branchName) ?? git.Run(["checkout", "-b", branchName]);
    }

    [Description("Check out an existing branch (git checkout), changing the working tree to match that branch's state. Uncommitted changes that conflict with the switch will block it rather than being silently discarded. Use CreateBranch instead if the branch does not exist yet.")]
    public string SwitchBranch(
        [Description("Name of the branch to switch to")] string branchName)
    {
        return Refused(nameof(branchName), branchName) ?? git.Run(["checkout", EndOfOptions, branchName]);
    }

    [Description("Delete a local branch (git branch -d, or -D when force is true). The safe default (force: false) refuses to delete a branch with unmerged commits; set force: true only when you specifically intend to discard that branch's unmerged work, since it cannot be recovered afterward through this tool. This only deletes the local branch reference — it does not touch any remote branch of the same name.")]
    public string DeleteBranch(
        [Description("Name of the branch to delete")] string branchName,
        [Description("If true, force delete even if not merged")] bool force = false)
    {
        return Refused(nameof(branchName), branchName) ?? git.Run(["branch", force ? "-D" : "-d", EndOfOptions, branchName]);
    }

    [Description("Merge the given branch or commit into the current branch (git merge), creating a merge commit if the histories have diverged, or fast-forwarding if possible. Conflicting changes will leave the merge unresolved and require manual conflict resolution — this tool does not resolve conflicts itself. Use Rebase instead if you want to replay the current branch's commits on top of another branch rather than merging them together.")]
    public string Merge(
        [Description("Branch name or commit to merge")] string source,
        [Description("Commit message for the merge; if empty, uses default")] string message = "")
    {
        if (Refused(nameof(source), source) is { } refused) return refused;
        return string.IsNullOrWhiteSpace(message)
            ? git.Run(["merge", EndOfOptions, source])
            : git.Run(["merge", "-m", message, EndOfOptions, source]);
    }

    [Description("Replay the current branch's commits on top of another branch or commit (git rebase), rewriting the current branch's history in the process. Set abort: true to cancel an in-progress rebase and return to the pre-rebase state instead of performing a new one — conflicts during a rebase will leave it in progress until resolved or aborted. Only rebase a branch that has not been pushed yet, or that nobody else is building on, since rebasing rewrites commits that may already be shared.")]
    public string Rebase(
        [Description("Branch or commit to rebase onto")] string onto,
        [Description("If true, abort an in-progress rebase")] bool abort = false)
    {
        if (abort) return git.Run(["rebase", "--abort"]);
        return Refused(nameof(onto), onto) ?? git.Run(["rebase", EndOfOptions, onto]);
    }

    [Description("Apply the changes from a specific commit onto the current branch as a new commit (git cherry-pick). This can conflict if the target branch doesn't have the context the commit was originally made against — conflicts are left unresolved for manual handling, same as Merge and Rebase. Use this to bring in one specific commit from another branch without merging or rebasing the whole branch.")]
    public string CherryPick(
        [Description("Commit hash or reference to cherry-pick")] string commitRef)
    {
        return Refused(nameof(commitRef), commitRef) ?? git.Run(["cherry-pick", EndOfOptions, commitRef]);
    }

    [Description("Create a tag pointing at the current commit (git tag). Without a message this creates a lightweight tag with just a name; with a message it creates an annotated tag with name, message, and tagger metadata (git tag -a). This only creates the tag locally — it is not pushed to the remote by this tool.")]
    public string CreateTag(
        [Description("Tag name")] string tagName,
        [Description("Optional message for an annotated tag")] string message = "")
    {
        if (Refused(nameof(tagName), tagName) is { } refused) return refused;
        return string.IsNullOrWhiteSpace(message)
            ? git.Run(["tag", EndOfOptions, tagName])
            : git.Run(["tag", "-a", "-m", message, EndOfOptions, tagName]);
    }

    [Description("List all tags in the repository (git tag). Read-only.")]
    public string ListTags()
    {
        return git.Run(["tag"]);
    }

    [Description("Delete a local tag by name (git tag -d). This only removes the tag locally — if it was already pushed to a remote, the remote copy is untouched and must be deleted separately.")]
    public string DeleteTag(
        [Description("Tag name to delete")] string tagName)
    {
        return Refused(nameof(tagName), tagName) ?? git.Run(["tag", "-d", EndOfOptions, tagName]);
    }
}
