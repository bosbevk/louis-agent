namespace LouisAgent.Core.Tools.Git.Running;

using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Git.Messages;

/// <summary>
/// The one way git runs (ADR 0004; S, CL15, from GitTools): each argument separate (no command line to split), in the
/// workspace or the folder ACP set (<c>AGENT_WORKING_DIRECTORY</c>), 30 seconds at most. A non-zero exit carries git's
/// own words. One per toolset, shared by its five tool classes.
/// </summary>
public sealed class GitRunner(string workspaceRoot)
{
    /// <param name="whenEmpty">The text when git printed nothing: any message enum (the git toolset's own, or the shared one); null for <c>ToolMessage.SucceededNoOutput</c>.</param>
    public string Run(IReadOnlyList<string> args, Enum? whenEmpty = null)
    {
        string workingDir = Environment.GetEnvironmentVariable("AGENT_WORKING_DIRECTORY") ?? workspaceRoot;
        var result = ProcessRunner.Run("git", args, workingDir, TimeSpan.FromSeconds(30));
        if (result.TimedOut) return MessageText.Format(GitToolMessage.GitTimeout);
        if (result.ExitCode != 0) return MessageText.Format(ToolMessage.ExitCode, new { Code = result.ExitCode, Output = result.Output.Trim() }).Trim();
        return string.IsNullOrWhiteSpace(result.Output) ? MessageText.Format(whenEmpty ?? ToolMessage.SucceededNoOutput) : ProcessRunner.Truncate(result.Output.Trim());
    }
}
