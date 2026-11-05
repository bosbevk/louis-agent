
using LouisAgent.Composition.Logging;
using LouisAgent.Core.Config;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Skills.Messages;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Core.Skills;
/// <summary>
/// Where skill files come from (S, CL15, from AgentHost): a built-in one from the assembly only, the agent's own
/// function files from the skills folder.
/// </summary>
public static class SkillFolder
{
    private static readonly ILogger Log = AgentLogging.LoggerFactory.CreateLogger(typeof(SkillFolder));

    /// <summary>
    /// The skills folder, where what the agent writes is kept (CreateSkillFile's agent/functions/, approved tools/):
    /// <see cref="AgentOptions.SkillsDirectory"/>, else the first <c>skills/</c> with its
    /// <see cref="SkillFiles.FolderMarker"/> in the working directory or its parents; null when there is none.
    /// </summary>
    public static string? Find(AgentOptions agent) => Find(agent, Environment.CurrentDirectory);

    /// <summary>The same, searching up from <paramref name="startDirectory"/>, so a test needn't move.</summary>
    internal static string? Find(AgentOptions agent, string startDirectory)
    {
        if (!string.IsNullOrWhiteSpace(agent.SkillsDirectory)) return agent.SkillsDirectory;
        for (var dir = new DirectoryInfo(startDirectory); dir is not null; dir = dir.Parent)
        {
            string marker = Path.GetFullPath(Path.Combine(dir.FullName, SkillFiles.MarkedFolderPath));
            if (File.Exists(marker)) return Path.GetDirectoryName(marker);
        }

        return null;
    }

    /// <summary>Every function skill file the agent wrote, by name.</summary>
    internal static IEnumerable<string> Discover(AgentOptions agent) =>
        Find(agent) is { } dir
        && Directory.Exists(Path.Combine(dir, SkillFiles.FunctionsFolder))
            ? Directory.EnumerateFiles(Path.Combine(dir, SkillFiles.FunctionsFolder), "*.md")
                .Order(StringComparer.OrdinalIgnoreCase)
            : [];

    /// <summary>
    /// A skill file's text: a built-in one from the assembly only (a copy in the skills folder is ignored, and logged), a
    /// function file the agent wrote from the skills folder; null when neither has it. A built-in file comes as written:
    /// a role's header (its own toolsets, F33) is for the loader to strip, never the model to see.
    /// </summary>
    internal static (string Text, bool FromDisk)? Read(AgentOptions agent, string fileName)
    {
        string? onDisk = Find(agent) is { } dir
            ? Path.Combine(dir, fileName.Replace('/', Path.DirectorySeparatorChar))
            : null;
        if (SkillFileSource.ReadEmbedded(fileName) is { } builtIn)
        {
            if (onDisk is not null && File.Exists(onDisk))
                Log.Log(LogLevel.Warning, SkillLogMessage.BuiltInCopyIgnored, new { File = onDisk });
            return (builtIn, false);
        }

        // Only the agent's own files are read from disk: a role or a guide is built in, or it isn't one.
        bool agentWritten = fileName.StartsWith(SkillFiles.FunctionsFolder + "/", StringComparison.Ordinal);
        return agentWritten && onDisk is not null && File.Exists(onDisk) ? (File.ReadAllText(onDisk), true) : null;
    }
}
