namespace LouisAgent.Core.Skills;

using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;

/// <summary>
/// Provides time tracking and project management skills for Paymo integration.
/// Loads skills from paymo-skills.md for time entry management.
/// </summary>
public class PaymoSkillsProvider : ISkillProvider
{
    private static readonly ILogger Log = AgentLogging.For<PaymoSkillsProvider>();

    private List<MarkdownSkillLoader.SkillDef>? _skills;
    private string? _documentation;
    private readonly string _filePath;

    public List<MarkdownSkillLoader.SkillDef> Skills => _skills ??= LoadSkills();
    public string Documentation => _documentation ??= LoadDocumentation();

    /// <param name="filePath">Path to paymo-skills.md file. Searches standard locations if not provided.</param>
    public PaymoSkillsProvider(string? filePath = null)
    {
        _filePath = ResolveFilePath(filePath ?? "paymo-skills.md");
    }

    private List<MarkdownSkillLoader.SkillDef> LoadSkills()
    {
        if (!File.Exists(_filePath))
        {
            Log.Log(LogLevel.Warning, LogMessage.SkillFileNotFound, new { File = "paymo-skills.md", Path = _filePath });
            return [];
        }

        var skills = MarkdownSkillLoader.Load(_filePath);
        Log.Log(LogLevel.Information, LogMessage.SkillsLoaded, new { skills.Count, Kind = "Paymo", Path = _filePath });
        return skills;
    }

    private string LoadDocumentation()
    {
        if (!File.Exists(_filePath))
        {
            Log.Log(LogLevel.Warning, LogMessage.SkillFileNotFound, new { File = "paymo-skills.md", Path = _filePath });
            return "";
        }

        try
        {
            return File.ReadAllText(_filePath);
        }
        catch (Exception ex)
        {
            Log.Log(LogLevel.Error, LogMessage.SkillFileLoadFailed, new { File = "paymo-skills.md", Reason = ex.Message });
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
