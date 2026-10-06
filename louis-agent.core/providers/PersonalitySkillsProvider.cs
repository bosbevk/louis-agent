namespace louis_agent.core.providers;

/// <summary>
/// The agent's personality: who it is and how it talks and thinks, kept apart from the operational instructions in
/// default.md so it can be changed (or left out) without touching how the agent works.
/// Loads personality.md; it comes first in the system prompt. A missing file just means no personality.
/// </summary>
public class PersonalitySkillsProvider : ISkillProvider
{
    private string? _documentation;
    private readonly string? _filePath;

    // personality.md holds no executable skills, only guidance.
    public List<MarkdownSkillLoader.SkillDef> Skills { get; } = [];
    public string Documentation => _documentation ??= LoadDocumentation();

    /// <param name="filePath">Path to personality.md, as found by AgentHost; null when there is none.</param>
    public PersonalitySkillsProvider(string? filePath)
    {
        _filePath = filePath;
    }

    private string LoadDocumentation()
    {
        if (_filePath is null || !File.Exists(_filePath))
        {
            Console.Error.WriteLine("[INFO] No personality.md found; running without a personality.");
            return "";
        }

        try
        {
            return File.ReadAllText(_filePath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ERROR] Failed to load personality.md: {ex.Message}");
            return "";
        }
    }
}
