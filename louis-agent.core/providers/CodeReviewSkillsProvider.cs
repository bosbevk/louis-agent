namespace louis_agent.core.providers;

/// <summary>
/// Provides code review and analysis skills.
/// Loads skills from code-review-skills.md for code quality assessment.
/// </summary>
public class CodeReviewSkillsProvider : ISkillProvider
{
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
            Console.Error.WriteLine($"[WARN] code-review-skills.md not found at: {_filePath}");
            return [];
        }

        var skills = MarkdownSkillLoader.Load(_filePath);
        Console.Error.WriteLine($"[INFO] Loaded {skills.Count} code review skills from: {_filePath}");
        return skills;
    }

    private string LoadDocumentation()
    {
        if (!File.Exists(_filePath))
        {
            Console.Error.WriteLine($"[WARN] code-review-skills.md not found at: {_filePath}");
            return "";
        }

        try
        {
            return File.ReadAllText(_filePath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ERROR] Failed to load code-review-skills.md: {ex.Message}");
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
