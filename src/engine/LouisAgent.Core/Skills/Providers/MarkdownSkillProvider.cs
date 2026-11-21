namespace LouisAgent.Core.Skills.Providers;

using LouisAgent.Core.Skills;

/// <summary>
/// One markdown skill file: its text is the documentation, its <c>## Skill:</c> procedures the skills. Who the agent is
/// and its routing come from <c>SkillLoader.Load</c>, which composes the files; a provider reads only its own.
/// </summary>
public class MarkdownSkillProvider : ISkillProvider
{
    private readonly List<MarkdownSkillLoader.SkillDef> _skills;
    private readonly string _documentation;

    public List<MarkdownSkillLoader.SkillDef> Skills => _skills;
    public string Documentation => _documentation;

    private MarkdownSkillProvider(List<MarkdownSkillLoader.SkillDef> skills, string documentation) =>
        (_skills, _documentation) = (skills, documentation);

    /// <summary>A skill file's text already read (from disk or the assembly, <see cref="SkillFileSource"/>), on its own.</summary>
    public static MarkdownSkillProvider FromText(string text) => new(MarkdownSkillLoader.Parse(text), text);

    /// <summary>A skill file on disk; a missing or blank one has no skills and no documentation.</summary>
    public MarkdownSkillProvider(string filePath)
    {
        _skills = MarkdownSkillLoader.Load(filePath);
        string text = File.Exists(filePath) ? File.ReadAllText(filePath) : "";
        _documentation = string.IsNullOrWhiteSpace(text) ? "" : text;
    }
}
