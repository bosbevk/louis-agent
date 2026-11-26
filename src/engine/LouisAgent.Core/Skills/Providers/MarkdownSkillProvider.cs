namespace LouisAgent.Core.Skills.Providers;

/// <summary>
/// One markdown file of the prompt, its text as it is. Who the agent is and its routing come from
/// <c>SkillLoader.Load</c>, which composes the files; a provider reads only its own.
/// </summary>
public class MarkdownSkillProvider : ISkillProvider
{
    /// <summary>A file on disk; a missing or blank one gives no text.</summary>
    public MarkdownSkillProvider(string filePath)
    {
        string text = File.Exists(filePath) ? File.ReadAllText(filePath) : "";
        Documentation = string.IsNullOrWhiteSpace(text) ? "" : text;
    }

    /// <summary>For <see cref="FromText"/>, which sets the text itself.</summary>
    private MarkdownSkillProvider()
    {
    }

    /// <summary>The file's text.</summary>
    public string Documentation { get; private init; } = "";

    /// <summary>
    /// A file's text already read (from disk or the assembly, <see cref="SkillFileSource"/>), on its own.
    /// </summary>
    public static MarkdownSkillProvider FromText(string text) => new() { Documentation = text };
}
