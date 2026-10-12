namespace LouisAgent.Core.Skills;

using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;

/// <summary>
/// Provides code review and analysis skills.
/// Loads skills from code-review-skills.md for code quality assessment.
/// </summary>
public class CodeReviewSkillsProvider : ISkillProvider
{
    private static readonly ILogger Log = AgentLogging.For<CodeReviewSkillsProvider>();

    private List<MarkdownSkillLoader.SkillDef>? _skills;
    private string? _documentation;
    private readonly string _filePath;

    public List<MarkdownSkillLoader.SkillDef> Skills => _skills ??= LoadSkills();
    public string Documentation => _documentation ??= LoadDocumentation();

    /// <param name="filePath">Path to code-review-skills.md file. Searches standard locations if not provided.</param>
    public CodeReviewSkillsProvider(string? filePath = null)
    {
        _filePath = ResolveFilePath(filePath ?? "code-review-skills.md");
    }

    private List<MarkdownSkillLoader.SkillDef> LoadSkills()
    {
        if (!File.Exists(_filePath))
        {
            Log.Log(LogLevel.Warning, LogMessage.SkillFileNotFound, new { File = "code-review-skills.md", Path = _filePath });
            return [];
        }

        var skills = MarkdownSkillLoader.Load(_filePath);
        Log.Log(LogLevel.Information, LogMessage.SkillsLoaded, new { skills.Count, Kind = "code review", Path = _filePath });
        return skills;
    }

    private string LoadDocumentation()
    {
        if (!File.Exists(_filePath))
        {
            Log.Log(LogLevel.Warning, LogMessage.SkillFileNotFound, new { File = "code-review-skills.md", Path = _filePath });
            return "";
        }

        try
        {
            return File.ReadAllText(_filePath);
        }
        catch (Exception ex)
        {
            Log.Log(LogLevel.Error, LogMessage.SkillFileLoadFailed, new { File = "code-review-skills.md", Reason = ex.Message });
            return "";
        }
    }

    private static string ResolveFilePath(string fileName)
    {
        var searchPaths = new[]
        {
            Path.Combine(Environment.CurrentDirectory, fileName),
            Path.Combine("/app", fileName),
            Path.Combine(AppContext.BaseDirectory, fileName)
        };

        return searchPaths.FirstOrDefault(File.Exists) ?? fileName;
    }
}
