
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Git.Messages;

namespace LouisAgent.Core.Tools.Git.Running;
/// <summary>
/// The one way git runs (ADR 0004; S, CL15, from GitTools): each argument separate (no command line to split), in the
/// workspace or the folder ACP set (<c>AGENT_WORKING_DIRECTORY</c>), 30 seconds at most. A non-zero exit carries git's
/// own words. One per toolset, shared by its five tool classes.
/// </summary>
public sealed class GitRunner(string workspaceRoot)
{
    /// <param name="whenEmpty">The git toolset's own text for when git printed nothing; null for <c>ToolMessage.SucceededNoOutput</c>.</param>
    public string Run(IReadOnlyList<string> args, GitToolMessage? whenEmpty = null)
    {
        string workingDir = Environment.GetEnvironmentVariable("AGENT_WORKING_DIRECTORY") ?? workspaceRoot;
        var result = ProcessRunner.Run("git", args, workingDir, TimeSpan.FromSeconds(30));
        if (result.TimedOut) return MessageText.Format(GitToolMessage.GitTimeout);
        if (result.ExitCode != 0)
            return MessageText
                .Format(ToolMessage.ExitCode, new { Code = result.ExitCode, Output = result.Output.Trim() }).Trim();
        if (!string.IsNullOrWhiteSpace(result.Output)) return ProcessRunner.Truncate(result.Output.Trim());
        return whenEmpty is { } own ? MessageText.Format(own) : MessageText.Format(ToolMessage.SucceededNoOutput);
    }
}
