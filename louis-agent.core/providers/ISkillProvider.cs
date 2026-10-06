namespace louis_agent.core.providers;

/// <summary>
/// Interface for providing skills to the agent engine.
/// Allows loading skills from different sources (files, APIs, databases, etc.)
/// </summary>
public interface ISkillProvider
{
    /// <summary>
    /// Gets the list of skill definitions.
    /// </summary>
    List<MarkdownSkillLoader.SkillDef> Skills { get; }

    /// <summary>
    /// Gets the skill documentation as a string.
    /// </summary>
    string Documentation { get; }
}
