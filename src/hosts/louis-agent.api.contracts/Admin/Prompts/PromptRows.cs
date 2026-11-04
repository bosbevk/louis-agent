namespace LouisAgent.Api.Contracts.Admin.Prompts;

/// <summary>Every built-in prompt this instance has, by its path (CL27).</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record PromptsReply(IReadOnlyList<PromptRow> Prompts);

/// <summary>
/// One built-in prompt as its header says it: its path under <c>Prompts/</c>, its name and description, the level a
/// mode, role or minion runs at (none for a guide or the principles), and the toolsets it always loads.
/// </summary>
public sealed record PromptRow(
    string Path,
    string Name,
    string Description,
    string? Level,
    IReadOnlyList<string> Toolsets);
