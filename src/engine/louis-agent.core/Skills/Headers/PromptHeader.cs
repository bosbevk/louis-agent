namespace LouisAgent.Core.Skills.Headers;

/// <summary>
/// What a built-in prompt says it is, in the front matter at its top (CL27): the Agent Skills spec's name (the file's
/// own) and description, and ours under <c>metadata</c>: the level a role or mode runs at, and the toolsets it always
/// loads (F33's <c>shops?</c>, a <c>?</c> loaded only where this instance has it). A guide or the principles name
/// neither.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0017-skills-by-level.md">ADR 0017: skill files follow the levels</seealso>
public sealed record PromptHeader(
    string Name,
    string Description,
    RoleLevel? Level,
    IReadOnlyList<string> Toolsets);
