using LouisAgent.Composition.Packs.Imports;
using LouisAgent.Composition.Packs.Screens;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Composition.Packs;

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
/// <item>Its guides are prose with no <c>## Skill:</c> procedures, so one can travel as an MCP server's
/// instructions.</item>
/// <item>It reads its own settings and core's shared services, names the packs it needs, and never references their
/// code.</item>
/// </list>
/// </summary>
public interface IAgentPack
{
    /// <summary>"shops": lowercase letters, digits and dashes.</summary>
    string Name { get; }

    /// <summary>The seam: an MCP server arrives as one more <see cref="PackToolset"/> (F36).</summary>
    IReadOnlyList<PackToolset> Toolsets { get; }

    /// <summary>
    /// Whether its toolsets come before the engine's own in a session's tools and prompt. The order is part of the
    /// prefix the model's cache keys on, so it stays fixed: general tools first, then the engine's, then a product's.
    /// </summary>
    bool ComesBeforeTheEngine => false;

    /// <summary>
    /// Its guides (<c>toolsets/{name}.md</c>) and, from CL16, its roles (the cook); over MCP only its tools convert, and
    /// a role runs with the identity that loads it (ADR 0028, rule 8).
    /// </summary>
    IFileProvider Prompts { get; }

    /// <summary>Other packs, by name: cooking requires "recipes".</summary>
    IReadOnlyList<string> Requires { get; }

    /// <summary>
    /// The data it publishes for anyone to read, apart from its tools (ADR 0070): none unless it says so.
    /// </summary>
    IReadOnlyList<IDataFeed> Feeds => [];

    /// <summary>
    /// The data a person may upload into it from the admin portal (F55): none unless it says so.
    /// </summary>
    IReadOnlyList<IDataImport> Imports => [];

    /// <summary>
    /// The screens it offers whoever runs it, behind the admin key, as plain data: none unless it says so.
    /// </summary>
    IReadOnlyList<IDataScreen> Screens => [];
}
