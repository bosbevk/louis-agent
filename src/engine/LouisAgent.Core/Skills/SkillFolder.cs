using LouisAgent.Composition.Logging;
using LouisAgent.Core.Config;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Skills.Messages;
using LouisAgent.Core.Skills.Messages.Holes.Prompts;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Core.Skills;

/// <summary>
/// Where the skills folder is, and where a prompt file comes from (S, CL15, from AgentHost): the assembly only.
/// </summary>
public static class SkillFolder
{
    private static readonly ILogger Log = AgentLogging.LoggerFactory.CreateLogger(typeof(SkillFolder));

    /// <summary>
    /// The skills folder, where the skills the agent keeps are, each a folder, and its drafts in <c>drafts/</c>:
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

    /// <summary>
    /// A prompt file's text, from the assembly only: a copy in the skills folder is ignored, and logged, since a role
    /// or a guide is built in or it isn't one. Null when the assembly hasn't it. It comes as written: a role's header
    /// (its own toolsets, F33) is for the loader to strip, never the model to see.
    /// </summary>
    internal static string? Read(AgentOptions agent, string fileName)
    {
        if (SkillFileSource.ReadEmbedded(fileName) is not { } builtIn) return null;

        string? onDisk = Find(agent) is { } dir
            ? Path.Combine(dir, fileName.Replace('/', Path.DirectorySeparatorChar))
            : null;
        if (onDisk is not null && File.Exists(onDisk))
        {
            var ignored = new BuiltInCopyIgnoredHoles(onDisk);
            Log.Log(LogLevel.Warning, SkillLogMessage.BuiltInCopyIgnored, ignored);
        }

        return builtIn;
    }
}
