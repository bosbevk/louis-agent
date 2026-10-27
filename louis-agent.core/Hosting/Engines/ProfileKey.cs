using LouisAgent.Choices;

using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Skills.Roles;

namespace LouisAgent.Core.Hosting.Engines;
/// <summary>
/// What one cached engine is for (F5, F21, CL8, F30): its toolsets' names joined with commas, its role, its level, its LLM profile and, for
/// a minion, its one tool. A value: two keys with the same parts are the same key (ADR 0040), so it holds only strings and an enum.
/// </summary>
public sealed record ProfileKey(string Toolsets, string? Role, RoleLevel Level, string? Llm, string? Tool)
{
    /// <summary>
    /// Empty means none, stated once: an empty <paramref name="role"/> or <paramref name="llm"/> is null, and so is a blank
    /// <paramref name="tool"/>, so two spellings of one request are one key. A blank role or LLM name is kept, to be refused as unknown.
    /// </summary>
    public static ProfileKey Of(IEnumerable<ToolsetDefinition> resolved, string? role, RoleLevel level, string? llm,
        string? tool) =>
        new(string.Join(",", resolved.Select(t => t.Name)), string.IsNullOrEmpty(role) ? null : role, level,
            string.IsNullOrEmpty(llm) ? null : llm, string.IsNullOrWhiteSpace(tool) ? null : tool);

    /// <summary>
    /// Exactly the text the factory used before (<c>Profiles</c> and the drop log show it): toolsets, then |role, |level below level 0,
    /// |llm:{profile} and |tool:{name} when set.
    /// </summary>
    public string Text => Toolsets + (Role is null ? "" : "|" + Role) + LevelSuffix(Level) +
                          (Llm is null ? "" : "|llm:" + Llm) + (Tool is null ? "" : "|tool:" + Tool);

    /// <summary>Level 0 adds nothing to the key, so its profiles keep the names they had before CL8.</summary>
    private static string LevelSuffix(RoleLevel level) => level == RoleLevel.Agent ? "" : "|" + level.Wire();
}
