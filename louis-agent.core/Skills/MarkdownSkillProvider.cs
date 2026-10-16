namespace LouisAgent.Core.Skills;

using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;

/// <summary>
/// Loads skills from a markdown file and combines with default instructions.
/// </summary>
public class MarkdownSkillProvider : ISkillProvider
{
    private static readonly ILogger Log = AgentLogging.For<MarkdownSkillProvider>();

    private readonly List<MarkdownSkillLoader.SkillDef> _skills;
    private readonly string _documentation;

    public List<MarkdownSkillLoader.SkillDef> Skills => _skills;
    public string Documentation => _documentation;

    private MarkdownSkillProvider(List<MarkdownSkillLoader.SkillDef> skills, string documentation) =>
        (_skills, _documentation) = (skills, documentation);

    /// <summary>A skill file's text already read (from disk or the assembly, <see cref="SkillFileSource"/>), on its own.</summary>
    public static MarkdownSkillProvider FromText(string text) => new(MarkdownSkillLoader.Parse(text), text);

    /// <param name="includeDefaultInstructions">
    /// Prepend default.md. Pass false for secondary providers in a composite so it is included only once.
    /// </param>
    public MarkdownSkillProvider(string filePath, bool includeDefaultInstructions = true)
    {
        // Load the specified skill file
        _skills = MarkdownSkillLoader.Load(filePath);

        // Combine default instructions with chosen skillset
        // Try multiple locations for default.md (current dir, Skills/, /app, or same dir as skillset)
        string defaultDocs = "";
        foreach (string searchPath in includeDefaultInstructions ? new[] { SkillFiles.Router, "Skills/" + SkillFiles.Router, "/app/" + SkillFiles.Router, "/app/Skills/" + SkillFiles.Router, Path.Combine(Path.GetDirectoryName(filePath) ?? "", SkillFiles.Router) } : Array.Empty<string>())
        {
            if (File.Exists(searchPath))
            {
                try
                {
                    defaultDocs = File.ReadAllText(searchPath);
                    Log.Log(LogLevel.Information, LogMessage.DefaultInstructionsLoaded, new { Path = searchPath });
                    break;
                }
                catch { }
            }
        }

        if (includeDefaultInstructions && string.IsNullOrEmpty(defaultDocs))
        {
            Log.Log(LogLevel.Warning, LogMessage.DefaultInstructionsNotFound);
        }

        string skillsetDocs = File.Exists(filePath) ? File.ReadAllText(filePath) : "";

        _documentation = string.Join(
            "\n\n",
            new[] { defaultDocs, skillsetDocs }.Where(content => !string.IsNullOrWhiteSpace(content)));
    }
}
