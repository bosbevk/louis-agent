namespace LouisAgent.Core.Agent;

using Microsoft.Extensions.FileProviders;

/// <summary>
/// What an agent can do (CL15, ADR 0027): one capability's toolsets, their guides, its own settings and state. A pack
/// stays convertible to an MCP server, both ways (ADR 0028), so nothing here assumes it runs in this process:
/// <list type="bullet">
/// <item>It is ready when built. Anything slow or async (a connection, a tool listing) happens before the catalogue is
/// built, once when the host starts, and its toolsets and tools never change afterwards (ADR 0006, 0008).</item>
/// <item>Its tools take plain data (strings, numbers, booleans, enums, lists and records of those; a
/// <see cref="CancellationToken"/> when they wait) and return text. A failure is text with its reason, never a thrown
/// exception: in our own loop the model sees a thrown message, but served over MCP the SDK replaces it.</item>
/// <item>Whatever a tool reads from the session (the user's place) also arrives as a parameter; no description names an
/// identity.</item>
/// <item>It frames the outside data it returns and bounds its own output: calls served over MCP skip the engine's
/// guards.</item>
/// <item>Its guides are prose with no <c>## Skill:</c> procedures, so one can travel as an MCP server's instructions.</item>
/// <item>It reads its own settings and core's shared services, names the packs it needs, and never references their
/// code.</item>
/// </list>
/// </summary>
public interface IAgentPack
{
    /// <summary>"shops": lowercase letters, digits and dashes.</summary>
    string Name { get; }

    /// <summary>The seam: an MCP server arrives as one more <see cref="ToolsetDefinition"/> (F36).</summary>
    IReadOnlyList<ToolsetDefinition> Toolsets { get; }

    /// <summary>
    /// Its guides (<c>toolsets/{name}.md</c>) and, from CL16, its roles (the cook); over MCP only its tools convert, and a
    /// role runs with the identity that loads it (ADR 0028, rule 8).
    /// </summary>
    IFileProvider Prompts { get; }

    /// <summary>Other packs, by name: cooking requires "recipes".</summary>
    IReadOnlyList<string> Requires { get; }
}
