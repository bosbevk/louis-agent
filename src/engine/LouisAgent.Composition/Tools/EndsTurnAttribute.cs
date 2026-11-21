namespace LouisAgent.Composition.Tools;

/// <summary>
/// Marks a tool that asks the person something, as the site pack's <c>Offer</c> does (FX138): once it succeeds, the
/// engine ends the turn there, so the model waits for their answer instead of running on.
/// </summary>
/// <remarks>
/// A marker the engine reads, as it reads <c>[Description]</c>: a pack stays plain tools with no reference to the
/// engine's chat client, and an MCP client, which can't end a turn, ignores it (ADR 0028). Louis's pick, of three (the
/// pack setting the chat client's flag itself; a rule in the prompt).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0028-packs-stay-convertible-to-mcp.md">ADR 0028: packs stay convertible to MCP</seealso>
[AttributeUsage(AttributeTargets.Method)]
public sealed class EndsTurnAttribute : Attribute;
