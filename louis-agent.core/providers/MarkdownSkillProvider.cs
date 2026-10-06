namespace louis_agent.core.providers;

/// <summary>
/// Loads skills from a markdown file and combines with default instructions.
/// </summary>
public class MarkdownSkillProvider : ISkillProvider
{
    private readonly List<MarkdownSkillLoader.SkillDef> _skills;
    private readonly string _documentation;

    public List<MarkdownSkillLoader.SkillDef> Skills => _skills;
    public string Documentation => _documentation;

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
        foreach (string searchPath in includeDefaultInstructions ? new[] { "default.md", "Skills/default.md", "/app/default.md", "/app/Skills/default.md", Path.Combine(Path.GetDirectoryName(filePath) ?? "", "default.md") } : Array.Empty<string>())
        {
            if (File.Exists(searchPath))
            {
                try
                {
                    defaultDocs = File.ReadAllText(searchPath);
                    Console.Error.WriteLine($"[INFO] Loaded default instructions from: {searchPath}");
                    break;
                }
                catch { }
            }
        }

        if (includeDefaultInstructions && string.IsNullOrEmpty(defaultDocs))
        {
            Console.Error.WriteLine($"[WARN] Could not find default.md in standard locations");
        }

        string skillsetDocs = File.Exists(filePath) ? File.ReadAllText(filePath) : "";

        _documentation = string.Join(
            "\n\n",
            new[] { defaultDocs, skillsetDocs }.Where(content => !string.IsNullOrWhiteSpace(content)));
    }
}
