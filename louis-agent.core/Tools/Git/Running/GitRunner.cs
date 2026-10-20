namespace LouisAgent.Core.Tools.Git.Running;

using LouisAgent.Core.Messages;

/// <summary>
/// The one way git runs (ADR 0004; S, CL15, from GitTools): each argument separate (no command line to split), in the
/// workspace or the folder ACP set (<c>AGENT_WORKING_DIRECTORY</c>), 30 seconds at most. A non-zero exit carries git's
/// own words. One per toolset, shared by its five tool classes.
/// </summary>
public sealed class GitRunner(string workspaceRoot)
{
    public string Run(IReadOnlyList<string> args, ToolMessage whenEmpty = ToolMessage.SucceededNoOutput)
    {
        string workingDir = Environment.GetEnvironmentVariable("AGENT_WORKING_DIRECTORY") ?? workspaceRoot;
        var result = ProcessRunner.Run("git", args, workingDir, TimeSpan.FromSeconds(30));
        if (result.TimedOut) return MessageText.Format(ToolMessage.GitTimeout);
        if (result.ExitCode != 0) return MessageText.Format(ToolMessage.ExitCode, new { Code = result.ExitCode, Output = result.Output.Trim() }).Trim();
        return string.IsNullOrWhiteSpace(result.Output) ? MessageText.Format(whenEmpty) : ProcessRunner.Truncate(result.Output.Trim());
    }
}
