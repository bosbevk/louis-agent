namespace LouisAgent.AcpServer.Paths;

using LouisAgent.AcpServer.Messages;
using LouisAgent.Core.Logging;
using Microsoft.Extensions.Logging;

/// <summary>
/// A Windows path from the IDE as the same file inside the container (S, CL15, from AcpServer): by ACP_MOUNT_MAPPINGS
/// (<c>C:\Users\you\RiderProjects=/repositories</c>) when one applies, else found under a mount root by its last path
/// parts, else the drive letter stripped; a file that isn't there is null, never guessed.
/// </summary>
/// <param name="mountMappings">The ACP_MOUNT_MAPPINGS value, read when asked, as the environment is.</param>
/// <param name="mountRoots">The roots searched when no mapping applies (/workspace, /repositories).</param>
internal sealed class ContainerPaths(Func<string?> mountMappings, IReadOnlyList<string> mountRoots)
{
    private static readonly ILogger Log = AgentLogging.For<ContainerPaths>();
    private const string Tag = "ACP";

    /// <summary>The mappings from the environment, and the roots a container mounts: /workspace (the main solution), /repositories (other repos).</summary>
    public static ContainerPaths FromEnvironment() =>
        new(() => Environment.GetEnvironmentVariable("ACP_MOUNT_MAPPINGS"), ["/workspace", "/repositories"]);

    /// <summary>The host path itself when it exists here (local dev), else the first candidate that does.</summary>
    public string? ResolveHostPath(string hostPath)
    {
        if (File.Exists(hostPath)) return hostPath;
        return Candidates(hostPath.Replace("\\", "/")).FirstOrDefault(File.Exists);
    }

    /// <summary>Where the file may be in the container, best first.</summary>
    public List<string> Candidates(string windowsPath)
    {
        var candidates = new List<string>();

        // Mount mappings (format: HOST_PATH1=/container/path1,HOST_PATH2=/container/path2)
        string? mappings = mountMappings();
        if (!string.IsNullOrWhiteSpace(mappings))
        {
            foreach (var mapping in mappings.Split(','))
            {
                var parts = mapping.Trim().Split('=');
                if (parts.Length == 2)
                {
                    string hostPath = parts[0].Replace("\\", "/");
                    string containerPath = parts[1];
                    if (windowsPath.StartsWith(hostPath, StringComparison.OrdinalIgnoreCase))
                    {
                        string relativePath = windowsPath.Substring(hostPath.Length);
                        candidates.Add(containerPath + relativePath);
                        Log.Log(LogLevel.Information, AcpLogMessage.CandidateFromMappings, new { Path = containerPath + relativePath }, tag: Tag);
                    }
                }
            }
        }

        // Fallback: the mount roots, then the path without its drive letter
        if (candidates.Count == 0)
        {
            foreach (string root in mountRoots)
            {
                if (!Directory.Exists(root)) continue;
                string? found = FindFileInDirectory(root, windowsPath);
                if (!string.IsNullOrWhiteSpace(found)) candidates.Add(found);
            }

            if (windowsPath.Length > 2 && windowsPath[1] == ':')
            {
                string pathWithoutDrive = windowsPath.Substring(2);
                candidates.Add(pathWithoutDrive);
            }
        }

        return candidates;
    }

    /// <summary>A file named like the Windows path's under <paramref name="containerRoot"/> whose last folders match it too.</summary>
    private static string? FindFileInDirectory(string containerRoot, string windowsPath)
    {
        string fileName = Path.GetFileName(windowsPath);
        try
        {
            var files = Directory.EnumerateFiles(containerRoot, fileName, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                // Verify it's the right file by checking parent directory names
                if (windowsPath.Contains("/"))
                {
                    string[] windowsParts = windowsPath.Split('/');
                    string[] containerParts = file.Replace("\\", "/").Split('/');

                    // Check if last few parts match
                    int matchCount = 0;
                    for (int i = 0; i < Math.Min(3, windowsParts.Length); i++)
                    {
                        int wIdx = windowsParts.Length - 1 - i;
                        int cIdx = containerParts.Length - 1 - i;
                        if (wIdx >= 0 && cIdx >= 0 && windowsParts[wIdx].Equals(containerParts[cIdx], StringComparison.OrdinalIgnoreCase))
                        {
                            matchCount++;
                        }
                    }

                    if (matchCount >= 2)
                    {
                        Log.Log(LogLevel.Information, AcpLogMessage.MatchingFile, new { Root = containerRoot, File = file }, tag: Tag);
                        return file;
                    }
                }
            }
        }
        catch
        {
            // Directory doesn't exist or can't be searched
        }

        return null;
    }
}
