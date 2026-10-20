namespace LouisAgent.Core.Tools.Git;

using System.ComponentModel;
using LouisAgent.Core.Tools.Git.Running;
using static LouisAgent.Core.Tools.Git.Running.GitArguments;

/// <summary>The git toolset's remotes (S, CL15: one of its five classes): push, pull, fetch, and the remotes themselves. Every value from the model passes <see cref="GitArguments"/> (FX19; a branch that is a refspec too) and runs on <see cref="GitRunner"/> (ADR 0004).</summary>
public sealed class GitRemoteTools(GitRunner git)
{
    [Description("Push local commits on the current branch to a remote (git push). Defaults to the origin remote and the current branch if none are specified. The force option pushes with --force-with-lease rather than a bare --force, which refuses if the remote has commits you haven't seen, but it can still overwrite a teammate's pushed work on that branch — only use it when that's clearly intended, such as after an amend or rebase on a branch nobody else is building on.")]
    public string Push(
        [Description("Remote name (default 'origin')")] string remote = "origin",
        [Description("Branch name; if empty, pushes current branch")] string branch = "",
        [Description("If true, force push (dangerous!)")] bool force = false)
    {
        if ((Refused(nameof(remote), remote) ?? RefusedRefspec(nameof(branch), branch)) is { } refused) return refused;
        var args = new List<string> { "push" };
        if (force) args.Add("--force-with-lease");
        args.AddRange([EndOfOptions, remote]);
        if (!string.IsNullOrWhiteSpace(branch)) args.Add(branch);
        return git.Run(args);
    }

    [Description("Fetch and merge the latest commits from a remote branch into the current branch (git pull). Defaults to origin and the current branch's upstream if none are specified. This can create a merge commit or conflict if local and remote history have diverged — use Fetch instead if you want to see what's new on the remote without merging it in yet.")]
    public string Pull(
        [Description("Remote name (default 'origin')")] string remote = "origin",
        [Description("Branch name; if empty, pulls current branch")] string branch = "")
    {
        if ((Refused(nameof(remote), remote) ?? RefusedRefspec(nameof(branch), branch)) is { } refused) return refused;
        var args = new List<string> { "pull", EndOfOptions, remote };
        if (!string.IsNullOrWhiteSpace(branch)) args.Add(branch);
        return git.Run(args);
    }

    [Description("Download the latest commits and refs from a remote without merging them into the current branch (git fetch). This updates the repository's view of the remote without touching the working directory. Use Pull instead if you also want those changes merged into your current branch.")]
    public string Fetch(
        [Description("Remote name (default 'origin')")] string remote = "origin")
    {
        return Refused(nameof(remote), remote) ?? git.Run(["fetch", EndOfOptions, remote]);
    }

    [Description("Return the URL configured for a remote (git remote get-url). Defaults to origin. Read-only; use AddRemote to add a new remote.")]
    public string GetRemoteUrl(
        [Description("Remote name (default 'origin')")] string remoteName = "origin")
    {
        return Refused(nameof(remoteName), remoteName) ?? git.Run(["remote", "get-url", EndOfOptions, remoteName]);
    }

    [Description("List all configured remotes and their fetch/push URLs (git remote -v). Read-only.")]
    public string ListRemotes()
    {
        return git.Run(["remote", "-v"]);
    }

    [Description("Add a new remote with the given name and URL (git remote add). Fails if a remote with that name already exists. Use GetRemoteUrl or ListRemotes first to check what's already configured.")]
    public string AddRemote(
        [Description("Name for the remote (e.g., 'origin')")] string name,
        [Description("URL for the remote")] string url)
    {
        return Refused(nameof(name), name) ?? Refused(nameof(url), url) ?? git.Run(["remote", "add", EndOfOptions, name, url]);
    }
}
