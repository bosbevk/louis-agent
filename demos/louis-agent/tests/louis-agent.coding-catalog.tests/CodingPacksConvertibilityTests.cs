using LouisAgent.Composition.Packs;
using LouisAgent.Testing.Packs;

namespace LouisAgent.Coding.Tests;

/// <summary>
/// Every pack the coding catalogue loads stays convertible to an MCP server (ADR 0028, F106.3.2 step 6): the triage
/// pack among them, held with the same shared check as Puls's packs.
/// </summary>
public class CodingPacksConvertibilityTests
{
    /// <summary>Every pack this catalogue loads, by name.</summary>
    private static IEnumerable<string> EveryPackName() => CodingPrompts.Catalog.Packs.Select(pack => pack.Name);

    /// <summary>
    /// A pack's tools keep their names, schemas and descriptions through the MCP SDK, and name no identity.
    /// </summary>
    [TestCaseSource(nameof(EveryPackName))]
    public void EveryPack_IsConvertibleToAnMcpServer(string name)
    {
        IAgentPack pack = CodingPrompts.Catalog.Packs.Single(candidate => candidate.Name == name);
        var identities = CodingPrompts.Catalog.Identities.Select(identity => identity.Name);

        var problems = PackConvertibility.Problems(pack, identities);

        Assert.That(problems, Is.Empty);
    }
}
