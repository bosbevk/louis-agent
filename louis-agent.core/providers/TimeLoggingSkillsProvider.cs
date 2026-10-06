namespace louis_agent.core.providers;

/// <summary>
/// Provides time entry and time tracking workflow skills.
/// Loads skills from time-logging-skills.md for time management guidance.
/// </summary>
public class TimeLoggingSkillsProvider : ISkillProvider
{
    private List<MarkdownSkillLoader.SkillDef>? _skills;
    private string? _documentation;
    private readonly string _filePath;

    public List<MarkdownSkillLoader.SkillDef> Skills => _skills ??= LoadSkills();
    public string Documentation => _documentation ??= LoadDocumentation();

    /// <param name="filePath">Path to time-logging-skills.md file. Searches standard locations if not provided.</param>
    public TimeLoggingSkillsProvider(string? filePath = null)
    {
        _filePath = ResolveFilePath(filePath ?? "time-logging-skills.md");
    }

    private List<MarkdownSkillLoader.SkillDef> LoadSkills()
    {
        if (!File.Exists(_filePath))
        {
            Console.Error.WriteLine($"[WARN] time-logging-skills.md not found at: {_filePath}");
            return [];
        }

        var skills = MarkdownSkillLoader.Load(_filePath);
        Console.Error.WriteLine($"[INFO] Loaded {skills.Count} time logging skills from: {_filePath}");
        return skills;
    }

    private string LoadDocumentation()
    {
        if (!File.Exists(_filePath))
        {
            Console.Error.WriteLine($"[WARN] time-logging-skills.md not found at: {_filePath}");
            return "";
        }

        try
        {
            return File.ReadAllText(_filePath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ERROR] Failed to load time-logging-skills.md: {ex.Message}");
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
