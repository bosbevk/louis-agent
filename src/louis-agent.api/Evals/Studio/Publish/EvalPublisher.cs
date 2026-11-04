using System.Diagnostics;
using System.Text;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;

namespace LouisAgent.Api.Evals.Studio.Publish;

/// <summary>What a publish made: the branch and the commit on it.</summary>
public sealed record Published(string Branch, string Commit);

/// <summary>
/// Commits an eval's folder beside its identity on a branch of its own, off the workspace's HEAD, without touching the
/// person's working tree or the branch they're on (F49.5, finding S): the push and the pull request are theirs.
/// </summary>
/// <remarks>
/// Git's plumbing over a temporary index (<c>GIT_INDEX_FILE</c>): <c>read-tree HEAD</c>, the folder's old files out
/// and its files in (<c>hash-object -w</c>, <c>update-index --cacheinfo</c>), then <c>write-tree</c>,
/// <c>commit-tree</c> and <c>update-ref</c>. So a publish never switches the checkout under the person, and a second
/// one replaces the folder on the branch. Arguments as data, never a shell (ADR 0004); the index is a temp file,
/// deleted after.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
public sealed class EvalPublisher(string workspaceRoot, TimeSpan? timeout = null)
{
    /// <summary>The longest any one git call may take: thirty seconds unless a test says otherwise.</summary>
    private readonly TimeSpan _timeout = timeout ?? TimeSpan.FromSeconds(30);

    /// <summary>Whether the workspace is a git checkout a publish can commit in.</summary>
    public bool CanCommit => Directory.Exists(Path.Combine(workspaceRoot, ".git")) ||
                             File.Exists(Path.Combine(workspaceRoot, ".git"));

    /// <summary>Commits the folder at the target on its branch, with this message, and says where.</summary>
    public async Task<Published> PublishAsync(string folder, PublishTarget target, string message)
    {
        string index = Path.Combine(Path.GetTempPath(), "louis-agent-publish-" + Guid.NewGuid().ToString("N"));
        try
        {
            string head = await GitAsync(index, "rev-parse", "HEAD");
            await GitAsync(index, "read-tree", head);
            // The folder's files on the branch are its files now: whatever was there before goes.
            await GitAsync(index, "rm", "--cached", "-r", "-q", "--ignore-unmatch", target.Folder);
            foreach (string file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).Order())
            {
                string blob = await GitAsync(index, "hash-object", "-w", file);
                string path = $"{target.Folder}/{Path.GetRelativePath(folder, file).Replace('\\', '/')}";
                await GitAsync(index, "update-index", "--add", "--cacheinfo", $"100644,{blob},{path}");
            }

            string tree = await GitAsync(index, "write-tree");
            string commit = await GitAsync(index, "commit-tree", tree, "-p", head, "-m", message);
            await GitAsync(index, "update-ref", $"refs/heads/{target.Branch}", commit);
            return new Published(target.Branch, commit);
        }
        finally
        {
            if (File.Exists(index)) File.Delete(index);
        }
    }

    /// <summary>Runs git in the workspace over the temporary index and hands back what it printed, trimmed.</summary>
    /// <remarks>
    /// A git that fails or takes too long throws in words, never as a cancellation: the route turns it into a 503.
    /// </remarks>
    private async Task<string> GitAsync(string index, params string[] args)
    {
        var start = new ProcessStartInfo("git")
        {
            WorkingDirectory = workspaceRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        start.Environment["GIT_INDEX_FILE"] = index;
        foreach (string arg in args) start.ArgumentList.Add(arg);

        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!await ExitsInTimeAsync(process))
            throw new InvalidOperationException(MessageText.Format(DraftMessage.PublishGitTimedOut,
                new
                {
                    Command = args[0],
                    Seconds = _timeout.TotalSeconds
                }));
        if (process.ExitCode != 0)
            throw new InvalidOperationException(MessageText.Format(DraftMessage.PublishGitFailed,
                new
                {
                    Command = args[0],
                    Reason = (await error).Trim()
                }));
        return (await output).Trim();
    }

    /// <summary>Whether git finished in time; one that didn't is stopped, so no git is left running.</summary>
    /// <remarks>
    /// The deadline decides, not the wait: <c>WaitForExitAsync</c> lets a git that has already exited through a token
    /// that has already run out, so a git faster than the check would count as in time even past its deadline, and a
    /// zero timeout would be a race (FX37). A deadline that has passed is a timeout, whoever got there first.
    /// </remarks>
    private async Task<bool> ExitsInTimeAsync(Process process)
    {
        using var deadline = new CancellationTokenSource(_timeout);
        try
        {
            await process.WaitForExitAsync(deadline.Token);
        }
        catch (OperationCanceledException)
        {
            // The deadline passed while git still ran: it is stopped below.
        }

        if (!deadline.IsCancellationRequested) return true;
        if (!process.HasExited) process.Kill(entireProcessTree: true);
        return false;
    }
}
