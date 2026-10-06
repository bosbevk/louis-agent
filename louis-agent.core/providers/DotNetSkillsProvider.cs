namespace louis_agent.core.providers;

/// <summary>
/// Provides guidance for the native DotNet* tools (build, test, run, debug workflows).
/// Loads dotnet-skills.md; always included because the DotNet* tools are always registered.
/// </summary>
public class DotNetSkillsProvider : ISkillProvider
{
    private List<MarkdownSkillLoader.SkillDef>? _skills;
    private string? _documentation;
    private readonly string _filePath;

    public List<MarkdownSkillLoader.SkillDef> Skills => _skills ??= LoadSkills();
    public string Documentation => _documentation ??= LoadDocumentation();

    /// <param name="filePath">Path to dotnet-skills.md file. Searches standard locations if not provided.</param>
    public DotNetSkillsProvider(string? filePath = null)
    {
        _filePath = ResolveFilePath(filePath ?? "dotnet-skills.md");
    }

    private List<MarkdownSkillLoader.SkillDef> LoadSkills()
    {
        if (!File.Exists(_filePath))
        {
            Console.Error.WriteLine($"[WARN] dotnet-skills.md not found at: {_filePath}");
            return [];
        }

        var skills = MarkdownSkillLoader.Load(_filePath);
        Console.Error.WriteLine($"[INFO] Loaded {skills.Count} .NET skills from: {_filePath}");
        return skills;
    }

    private string LoadDocumentation()
    {
        if (!File.Exists(_filePath))
        {
            Console.Error.WriteLine($"[WARN] dotnet-skills.md not found at: {_filePath}");
            return "";
        }

        try
        {
            return File.ReadAllText(_filePath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ERROR] Failed to load dotnet-skills.md: {ex.Message}");
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
