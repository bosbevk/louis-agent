using LouisAgent.Composition.Packs;
using LouisAgent.Testing.Packs;

namespace LouisAgent.Coding.Tests;

/// <summary>
/// Every pack the coding catalogue loads stays convertible to an MCP server (ADR 0028, F106.3.2 step 6): the triage
/// pack among them, held with the same shared check as Puls's packs.
/// </summary>
public class CodingPacksConvertibilityTests
{
    /// <summary>
    /// The one gap the check found when it first ran over this catalogue (FX83 in the TODO): the paymo and devops
    /// packs' async tools take no CancellationToken. Named here so the gap is visible and nothing else hides behind it;
    /// the day they take one, this list goes.
    /// </summary>
    private static readonly string[] KnownGaps = ["paymo", "devops"];

    private const string MissingToken = ": waits but takes no CancellationToken";

    private static IEnumerable<string> EveryPackName() => CodingPrompts.Catalog.Packs.Select(pack => pack.Name);

    /// <summary>A pack's tools keep their names, schemas and descriptions through the MCP SDK, and name no identity.</summary>
    [TestCaseSource(nameof(EveryPackName))]
    public void EveryPack_IsConvertibleToAnMcpServer(string name)
    {
        IAgentPack pack = CodingPrompts.Catalog.Packs.Single(candidate => candidate.Name == name);
        var identities = CodingPrompts.Catalog.Identities.Select(identity => identity.Name);

        var problems = PackConvertibility.Problems(pack, identities);

        if (KnownGaps.Contains(name))
        {
            // The known gap, and only it: every problem is the missing token, and there is at least one until FX83 lands.
            Assert.That(problems, Has.All.EndsWith(MissingToken).And.Not.Empty, "the gap is still open; when it closes, take the pack off KnownGaps");
            return;
        }

        Assert.That(problems, Is.Empty);
    }
}
