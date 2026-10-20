namespace LouisAgent.AcpServer.Paths;

using LouisAgent.AcpServer.Messages;
using LouisAgent.Core.Logging;
using Microsoft.Extensions.Logging;

/// <summary>The repository a file from the IDE belongs to (S, CL15, from AcpServer): the nearest folder above it with a .git.</summary>
internal sealed class GitRoots(ContainerPaths paths)
{
    private static readonly ILogger Log = AgentLogging.For<GitRoots>();
    private const string Tag = "ACP";

    /// <summary>The root, straight from the path when it exists here, else through the container mapping; empty when there is none.</summary>
    public string Find(string filePath)
    {
        // Normalize path separators
        string workingPath = filePath.Replace("\\", "/");
        Log.Log(LogLevel.Information, AcpLogMessage.FindGitRootNormalized, new { Path = workingPath }, tag: Tag);

        // Try direct path first (works for local dev, not Docker)
        Log.Log(LogLevel.Information, AcpLogMessage.TryingDirectPath, tag: Tag);
        var dirInfo = new DirectoryInfo(Path.GetDirectoryName(workingPath) ?? workingPath);
        while (dirInfo != null)
        {
            string gitPath = Path.Combine(dirInfo.FullName, ".git");
            if (Directory.Exists(gitPath))
            {
                Log.Log(LogLevel.Information, AcpLogMessage.FoundGitDirect, new { Path = dirInfo.FullName }, tag: Tag);
                return dirInfo.FullName;
            }
            Log.Log(LogLevel.Information, AcpLogMessage.NoGitAt, new { Path = dirInfo.FullName }, tag: Tag);
            dirInfo = dirInfo.Parent;
        }

        // Path not found directly - we're in Docker, need to map Windows paths to container paths
        Log.Log(LogLevel.Information, AcpLogMessage.DirectPathExhausted, tag: Tag);
        var containerPaths = paths.Candidates(workingPath);
        Log.Log(LogLevel.Information, AcpLogMessage.CandidatesGenerated, new { containerPaths.Count }, tag: Tag);

        foreach (var containerPath in containerPaths)
        {
            Log.Log(LogLevel.Information, AcpLogMessage.TryingContainerPath, new { Path = containerPath }, tag: Tag);
            dirInfo = new DirectoryInfo(Path.GetDirectoryName(containerPath) ?? containerPath);
            int depth = 0;
            while (dirInfo != null && depth < 10)
            {
                string gitPath = Path.Combine(dirInfo.FullName, ".git");
                Log.Log(LogLevel.Information, AcpLogMessage.CheckingDepth, new { Depth = depth, Path = dirInfo.FullName }, tag: Tag);
                if (Directory.Exists(gitPath))
                {
                    Log.Log(LogLevel.Information, AcpLogMessage.FoundGitMapped, new { Path = dirInfo.FullName }, tag: Tag);
                    return dirInfo.FullName;
                }
                dirInfo = dirInfo.Parent;
                depth++;
            }
        }

        Log.Log(LogLevel.Warning, AcpLogMessage.NoGitFound, tag: Tag);
        return "";
    }
}
