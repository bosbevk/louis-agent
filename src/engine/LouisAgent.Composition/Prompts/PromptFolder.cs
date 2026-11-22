using LouisAgent.Choices;

namespace LouisAgent.Composition.Prompts;

/// <summary>
/// The folders a prompt sits in, at a project's root or inside an agent's folder: the one set of names the engine,
/// every identity and every pack share, so a path is built and read the same way everywhere.
/// </summary>
/// <remarks>
/// An enum, not constants: a folder's name repeats wherever a path is built or read, and a switch over the enum is one
/// the compiler can check (ADR 0043). It lives in Composition because where a prompt sits is the contract an identity
/// or a pack follows to be read, not an engine detail (Louis's pick B, over a leaf folder in core's Skills/ and over
/// moving a file to make room).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0133-subagents-live-inside-their-agent.md">ADR 0133: subagents live inside their agent</seealso>
public enum PromptFolder
{
    /// <summary>The agents, a folder each: <c>agents/{identity}/voice.md</c>.</summary>
    [WireName("agents")] Agents,

    /// <summary>An agent's subagents, inside its folder: <c>agents/{identity}/subagents/{name}/voice.md</c>.</summary>
    [WireName("subagents")] Subagents,

    /// <summary>An agent's skills, inside its folder: <c>agents/{identity}/skills/{name}/SKILL.md</c>.</summary>
    [WireName("skills")] Skills,

    /// <summary>The function skills the agent runs and writes: <c>agents/functions/</c>.</summary>
    [WireName("functions")] Functions,

    /// <summary>
    /// An agent's lookup agents, inside its folder apart from its subagents (CL68.3, ADR 0021):
    /// <c>agents/{identity}/lookups/{name}/voice.md</c>.
    /// </summary>
    [WireName("lookups")] Lookups,

    /// <summary>The toolsets' guides: <c>toolsets/{name}.md</c>.</summary>
    [WireName("toolsets")] Toolsets,

    /// <summary>What every agent loads: <c>shared/principles.md</c>.</summary>
    [WireName("shared")] Shared,
}
