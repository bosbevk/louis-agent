namespace louis_agent.core.tools;

using System.ComponentModel;
using System.Diagnostics;
using System.Text;

public sealed class GitTools
{
    private readonly string _workspaceRoot;

    public GitTools(string workspaceRoot)
    {
        _workspaceRoot = workspaceRoot;
    }

    [Description("Get the current git branch name.")]
    public string GetCurrentBranch()
    {
        return RunGit("rev-parse --abbrev-ref HEAD");
    }

    [Description("Get the current git status (staged, unstaged, untracked files).")]
    public string GetStatus()
    {
        return RunGit("status --short");
    }

    [Description("Get the recent commit log with specified number of entries.")]
    public string GetLog(
        [Description("Number of recent commits to show (default 10)")] int count = 10)
    {
        return RunGit($"log --oneline -n {count}");
    }

    [Description("Show details of a specific commit.")]
    public string ShowCommit(
        [Description("Commit hash or reference (e.g., HEAD, HEAD~1)")] string commitRef)
    {
        return RunGit($"show {commitRef} --stat");
    }

    [Description("Get a list of all branches (local and remote).")]
    public string ListBranches(
        [Description("If true, show all branches (local + remote); if false, show only local")] bool all = false)
    {
        return RunGit(all ? "branch -a" : "branch");
    }

    [Description("Create a new branch from the current HEAD.")]
    public string CreateBranch(
        [Description("Name for the new branch")] string branchName)
    {
        return RunGit($"checkout -b {branchName}");
    }

    [Description("Switch to an existing branch.")]
    public string SwitchBranch(
        [Description("Name of the branch to switch to")] string branchName)
    {
        return RunGit($"checkout {branchName}");
    }

    [Description("Delete a branch (safe; fails if not fully merged).")]
    public string DeleteBranch(
        [Description("Name of the branch to delete")] string branchName,
        [Description("If true, force delete even if not merged")] bool force = false)
    {
        return RunGit($"branch {(force ? "-D" : "-d")} {branchName}");
    }

    [Description("Stage files for commit.")]
    public string Stage(
        [Description("File path(s) to stage (use '.' for all changes)")] string pathSpec = ".")
    {
        return RunGit($"add {pathSpec}");
    }

    [Description("Unstage files.")]
    public string Unstage(
        [Description("File path(s) to unstage (use '.' for all staged changes)")] string pathSpec = ".")
    {
        return RunGit($"reset {pathSpec}");
    }

    [Description("Commit staged changes with a message.")]
    public string Commit(
        [Description("Commit message")] string message)
    {
        return RunGit($"commit -m \"{EscapeForShell(message)}\"");
    }

    [Description("Amend the previous commit without changing the message.")]
    public string AmendCommit(
        [Description("If true, keep the previous commit message; if false, allow editing")] bool noEdit = true)
    {
        return RunGit($"commit --amend {(noEdit ? "--no-edit" : "")}");
    }

    [Description("Show diff of unstaged changes.")]
    public string DiffUnstaged(
        [Description("Optional file path to show diff for a specific file")] string filePath = "")
    {
        return RunGit($"diff {filePath}".Trim());
    }

    [Description("Show diff of staged changes.")]
    public string DiffStaged(
        [Description("Optional file path to show diff for a specific file")] string filePath = "")
    {
        return RunGit($"diff --cached {filePath}".Trim());
    }

    [Description("Discard unstaged changes in working directory.")]
    public string DiscardUnstaged(
        [Description("File path(s) to discard (use '.' for all changes)")] string pathSpec = ".")
    {
        return RunGit($"checkout -- {pathSpec}");
    }

    [Description("Discard all staged changes and reset working directory.")]
    public string ResetHard(
        [Description("Commit/branch to reset to (default HEAD)")] string target = "HEAD")
    {
        return RunGit($"reset --hard {target}");
    }

    [Description("Stash current changes (staged and unstaged).")]
    public string Stash(
        [Description("Optional description for the stash")] string message = "")
    {
        return string.IsNullOrWhiteSpace(message)
            ? RunGit("stash")
            : RunGit($"stash save \"{EscapeForShell(message)}\"");
    }

    [Description("List all stashed changes.")]
    public string ListStashes()
    {
        return RunGit("stash list");
    }

    [Description("Apply or pop a stashed change.")]
    public string ApplyStash(
        [Description("Stash reference (e.g., stash@{0}); if empty, applies most recent")] string stashRef = "stash@{0}",
        [Description("If true, pop (remove) the stash; if false, just apply")] bool pop = false)
    {
        return RunGit($"stash {(pop ? "pop" : "apply")} {stashRef}");
    }

    [Description("Push commits to a remote branch.")]
    public string Push(
        [Description("Remote name (default 'origin')")] string remote = "origin",
        [Description("Branch name; if empty, pushes current branch")] string branch = "",
        [Description("If true, force push (dangerous!)")] bool force = false)
    {
        string branchPart = string.IsNullOrWhiteSpace(branch) ? "" : branch;
        string forceFlag = force ? " --force-with-lease" : "";
        return RunGit($"push {remote} {branchPart}{forceFlag}".Trim());
    }

    [Description("Pull latest changes from a remote branch.")]
    public string Pull(
        [Description("Remote name (default 'origin')")] string remote = "origin",
        [Description("Branch name; if empty, pulls current branch")] string branch = "")
    {
        string branchPart = string.IsNullOrWhiteSpace(branch) ? "" : branch;
        return RunGit($"pull {remote} {branchPart}".Trim());
    }

    [Description("Fetch updates from remote without merging.")]
    public string Fetch(
        [Description("Remote name (default 'origin')")] string remote = "origin")
    {
        return RunGit($"fetch {remote}");
    }

    [Description("Merge a branch into the current branch.")]
    public string Merge(
        [Description("Branch name or commit to merge")] string source,
        [Description("Commit message for the merge; if empty, uses default")] string message = "")
    {
        string msgPart = string.IsNullOrWhiteSpace(message) ? "" : $"-m \"{EscapeForShell(message)}\"";
        return RunGit($"merge {source} {msgPart}".Trim());
    }

    [Description("Rebase current branch onto another branch.")]
    public string Rebase(
        [Description("Branch or commit to rebase onto")] string onto,
        [Description("If true, abort an in-progress rebase")] bool abort = false)
    {
        return abort ? RunGit("rebase --abort") : RunGit($"rebase {onto}");
    }

    [Description("Create a tag at the current commit.")]
    public string CreateTag(
        [Description("Tag name")] string tagName,
        [Description("Optional message for an annotated tag")] string message = "")
    {
        return string.IsNullOrWhiteSpace(message)
            ? RunGit($"tag {tagName}")
            : RunGit($"tag -a {tagName} -m \"{EscapeForShell(message)}\"");
    }

    [Description("List all tags.")]
    public string ListTags()
    {
        return RunGit("tag");
    }

    [Description("Delete a tag.")]
    public string DeleteTag(
        [Description("Tag name to delete")] string tagName)
    {
        return RunGit($"tag -d {tagName}");
    }

    [Description("Get the URL of the remote repository.")]
    public string GetRemoteUrl(
        [Description("Remote name (default 'origin')")] string remoteName = "origin")
    {
        return RunGit($"remote get-url {remoteName}");
    }

    [Description("List all remotes.")]
    public string ListRemotes()
    {
        return RunGit("remote -v");
    }

    [Description("Add a new remote.")]
    public string AddRemote(
        [Description("Name for the remote (e.g., 'origin')")] string name,
        [Description("URL for the remote")] string url)
    {
        return RunGit($"remote add {name} {url}");
    }

    [Description("Show information about the repository (branch, remote, tracking).")]
    public string GetRepositoryInfo()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Repository Info ===");
        sb.AppendLine($"Current Branch: {GetCurrentBranch()}");
        sb.AppendLine($"Remote(s): {ListRemotes()}");
        sb.AppendLine($"Status:\n{GetStatus()}");
        return sb.ToString();
    }

    [Description("Cherry-pick a commit onto the current branch.")]
    public string CherryPick(
        [Description("Commit hash or reference to cherry-pick")] string commitRef)
    {
        return RunGit($"cherry-pick {commitRef}");
    }

    [Description("Check if there are any uncommitted changes.")]
    public string HasUncommittedChanges()
    {
        string status = RunGit("status --porcelain");
        return string.IsNullOrWhiteSpace(status) ? "No uncommitted changes." : $"Uncommitted changes found:\n{status}";
    }

    [Description("Get the git configuration (user name, email, etc.).")]
    public string GetConfig(
        [Description("Config key (e.g., 'user.name'); if empty, shows all config")] string key = "")
    {
        return string.IsNullOrWhiteSpace(key) ? RunGit("config --list") : RunGit($"config {key}");
    }

    [Description("Set a git configuration value.")]
    public string SetConfig(
        [Description("Config key (e.g., 'user.name')")] string key,
        [Description("Config value")] string value)
    {
        return RunGit($"config {key} \"{EscapeForShell(value)}\"");
    }

    private string RunGit(string args)
    {
        try
        {
            // Check for dynamic working directory (set by ACP when file is from different repo)
            string workingDir = Environment.GetEnvironmentVariable("AGENT_WORKING_DIRECTORY") ?? _workspaceRoot;

            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = args,
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(30_000))
            {
                process.Kill(entireProcessTree: true);
                return "[Timeout] Git command exceeded 30s limit.";
            }

            string output = stdout.GetAwaiter().GetResult();
            string error = stderr.GetAwaiter().GetResult();

            if (process.ExitCode != 0)
            {
                return $"[Exit Code {process.ExitCode}] {(string.IsNullOrWhiteSpace(error) ? output : error)}".Trim();
            }

            return string.IsNullOrWhiteSpace(output) ? "Success (no output)." : output.Trim();
        }
        catch (Exception ex)
        {
            return $"Error running git: {ex.Message}";
        }
    }

    private static string EscapeForShell(string input)
    {
        return input.Replace("\"", "\\\"");
    }
}
