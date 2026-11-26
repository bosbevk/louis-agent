using System.Text.Json;

namespace LouisAgent.Core.Skills.Headers.Agent.Drafting;

/// <summary>
/// The call a skill was proved with when it was made, and what it answered (F97.1), kept in its
/// <c>examples/example.json</c> so a person approving it, and the model reading it, see it work.
/// </summary>
/// <param name="Arguments">The call's arguments, as the model gave them.</param>
/// <param name="Answer">What the skill answered.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record SkillExample(JsonElement Arguments, string Answer)
{
    /// <summary>How the file is written: indented for a person, its keys as JSON's own camel case.</summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
}
