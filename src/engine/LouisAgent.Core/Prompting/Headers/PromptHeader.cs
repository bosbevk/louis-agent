namespace LouisAgent.Core.Prompting.Headers;

/// <summary>
/// What a prompt or a skill says it is, in the front matter at its top (CL27): the Agent Skills spec's name (the
/// file's own, or a skill's folder's) and description, and ours under <c>metadata</c>: the level a role or mode runs
/// at, the toolsets it always loads (F33's <c>shops?</c>, a <c>?</c> loaded only where this instance has it), how an
/// identity's skill loads, and the script a skill the agent made runs, by its path from the skill's folder (F97.1). A
/// guide or the principles name none of them.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0017-skills-by-level.md">ADR 0017: skill files follow the levels</seealso>
public sealed record PromptHeader(
    string Name,
    string Description,
    RoleLevel? Level,
    IReadOnlyList<string> Toolsets,
    SkillLoad? Load = null,
    string? Script = null);
