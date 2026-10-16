namespace LouisAgent.Core.Skills;

using System.Reflection;

/// <summary>
/// Reads a built-in skill file by name (F5 step 4b): a file of that name on disk first, so a copy in the skills folder
/// overrides one without a rebuild, else the copy embedded in louis-agent.core, so a host finds personality.md,
/// default.md and every toolset's guide even with no Skills/ folder. Files the agent writes stay on disk only.
/// </summary>
public static class SkillFileSource
{
    // The csproj embeds Skills/*.md as "Skills.{file name}".
    private const string Prefix = "Skills.";

    private static readonly Assembly Assembly = typeof(SkillFileSource).Assembly;

    /// <summary>The file names embedded in the assembly.</summary>
    public static IReadOnlyList<string> EmbeddedNames { get; } = Assembly.GetManifestResourceNames()
        .Where(n => n.StartsWith(Prefix, StringComparison.Ordinal))
        .Select(n => n[Prefix.Length..])
        .Order(StringComparer.Ordinal)
        .ToList();

    /// <summary>The embedded copy of <paramref name="fileName"/>, or null when it isn't built in.</summary>
    public static string? ReadEmbedded(string fileName)
    {
        // A file in a subfolder (agent/puls.md) is embedded as Skills.agent.puls.md.
        using Stream? stream = Assembly.GetManifestResourceStream(Prefix + fileName.Replace('/', '.'));
        if (stream is null) return null;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// The text of <paramref name="fileName"/> from the first of <paramref name="diskPaths"/> that exists, else the
    /// embedded copy; null when neither has it. <c>FromDisk</c> says which.
    /// </summary>
    public static (string Text, bool FromDisk)? Read(string fileName, IEnumerable<string> diskPaths)
    {
        if (diskPaths.FirstOrDefault(File.Exists) is { } path) return (File.ReadAllText(path), true);
        return ReadEmbedded(fileName) is { } embedded ? (embedded, false) : null;
    }
}