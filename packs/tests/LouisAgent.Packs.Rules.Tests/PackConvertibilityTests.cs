using LouisAgent.Composition.Packs;
using LouisAgent.Testing.Packs;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Packs.Rules.Tests;

/// <summary>
/// Every pack stays convertible to an MCP server (ADR 0028), held with the shared check: each tool keeps its name,
/// its input schema and its description through the MCP SDK's own wrapper, returns text, takes plain data and a
/// cancellation token when it waits, carries a safe name and names no identity, and the pack's guides are prose in
/// its own prompts. What it can't see is ADR 0028's review checklist.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0028-packs-stay-convertible-to-mcp.md">ADR 0028: packs stay convertible to MCP</seealso>
public class PackConvertibilityTests
{
    /// <summary>The identity a tool may not name in the fake pack below.</summary>
    private const string FakeIdentity = "Acme";

    /// <summary>Every pack, by name.</summary>
    private static IEnumerable<string> EveryPackName() => EveryPack.All.Select(pack => pack.Name);

    /// <summary>
    /// The identities in this repository, by the short names their folders stand for, which no pack's tool may
    /// carry: each demo's under its <c>identity/</c>, and the site's under <c>src/site</c>.
    /// </summary>
    private static IReadOnlyList<string> Identities() =>
    [
        .. TestPaths.Demos().Select(demo => Path.Combine(demo, "identity")).Append(Path.Combine(TestPaths.RepoRoot,
                "src/site"))
            .Where(Directory.Exists)
            .SelectMany(Directory.EnumerateDirectories)
            .Select(Path.GetFileName).OfType<string>()
            .Where(name => name.StartsWith(ProjectNames.IdentityPrefix, StringComparison.Ordinal))
            .Select(name => ProjectNames.ShortName(name, ProjectNames.IdentityPrefix))
    ];

    /// <summary>
    /// The packs whose waiting tools take no cancellation token yet, found when this check first held every pack
    /// (F166.4): FX83 gives them one. The list only shrinks: a pack leaves it when its tools are fixed.
    /// </summary>
    private static readonly HashSet<string> WaitWithoutAToken = ["devops", "paymo"];

    /// <summary>What a tool that waits without a cancellation token is reported as.</summary>
    private const string NoToken = "waits but takes no CancellationToken";

    /// <summary>
    /// A pack's tools come through the MCP SDK whole, and name no identity; a pack on the list above is excused only
    /// its missing tokens.
    /// </summary>
    [TestCaseSource(nameof(EveryPackName))]
    public void EveryPack_IsConvertibleToAnMcpServer(string name)
    {
        var problems = PackConvertibility.Problems(EveryPack.Named(name), Identities());
        var excused = WaitWithoutAToken.Contains(name) ? problems.Where(p => !p.EndsWith(NoToken)) : problems;

        Assert.That(excused, Is.Empty);
    }

    /// <summary>Every pack on the list still has a tool without a token, so the list can't go stale.</summary>
    [TestCaseSource(nameof(WaitWithoutAToken))]
    public void AnExcusedPack_StillWaitsWithoutAToken(string name) =>
        Assert.That(PackConvertibility.Problems(EveryPack.Named(name), Identities()),
            Has.Some.EndsWith(NoToken), $"{name} is fixed: take it off the list");

    /// <summary>The identities were found, so the check above has names to refuse.</summary>
    [Test]
    public void TheIdentities_AreFound() => Assert.That(Identities(), Has.Count.GreaterThanOrEqualTo(4));

    /// <summary>A pack that breaks every rule once is reported with exactly its problems.</summary>
    [Test]
    public void AnUnconvertiblePack_ListsExactlyItsProblems()
    {
        var problems = PackConvertibility.Problems(new FakeUnconvertiblePack(), [FakeIdentity]);

        Assert.That(problems, Is.EquivalentTo(new[]
        {
            "fakebad/FindForAcme: its description names the identity 'Acme'",
            "fakebad/FindForAcme: parameter 'area' names the identity 'Acme'",
            "fakebad/Count: returns Int32, not string, Task<string> or ValueTask<string>",
            "fakebad/Read: parameter 'http' is HttpClient, not plain data",
            "fakebad/Wait: waits but takes no CancellationToken",
            "fakebad/AToolNameOfSixtyFiveCharactersIsOneMoreThanAProviderAcceptsSafelyXYZ: its name isn't " +
            PackConvertibility.NameRule,
            "fakebad: toolsets/fakebad.md holds a '## Skill:' procedure; " +
            "a pack's guide is prose, a procedure becomes a tool",
            "fakenoguide: toolsets/fakenoguide.md isn't in the pack's prompts",
        }));
    }
}

/// <summary>A pack that breaks every rule once, so the list of problems is checked against a known set.</summary>
internal sealed class FakeUnconvertiblePack : IAgentPack
{
    public string Name => "fakeunconvertible";

    public IReadOnlyList<PackToolset> Toolsets { get; } =
    [
        new("fakebad", "Breaks the tool rules", GuideFile: "toolsets/fakebad.md", MissingSettings: _ => [],
            CreateTools: _ => [new FakeUnconvertibleTools()], ToolTypes: [typeof(FakeUnconvertibleTools)]),
        new("fakenoguide", "Its guide isn't in its prompts", GuideFile: "toolsets/fakenoguide.md",
            MissingSettings: _ => [], CreateTools: _ => [new FakeFineTools()], ToolTypes: [typeof(FakeFineTools)]),
    ];

    public IFileProvider Prompts { get; } = new FakePrompts(new Dictionary<string, string>
    {
        ["toolsets/fakebad.md"] =
            "# Fakebad\n\nA guide with a procedure.\n\n## Skill: Greet\n\n- Execution:\n\n```bash\necho hi\n```\n",
    });

    public IReadOnlyList<string> Requires => [];
}

/// <summary>Tools that each break one rule.</summary>
public sealed class FakeUnconvertibleTools
{
    /// <summary>Names an identity in its description and its parameter.</summary>
    [System.ComponentModel.Description("Finds what Acme asked for.")]
    public string FindForAcme([System.ComponentModel.Description("The town, when Acme gave none")] string area) => area;

    /// <summary>Returns a number, not text.</summary>
    [System.ComponentModel.Description("Counts, in a number.")]
    public int Count([System.ComponentModel.Description("What to count")] string query) => query.Length;

    /// <summary>Takes a client, not plain data.</summary>
    [System.ComponentModel.Description("Reads through a client it is handed.")]
    public string Read([System.ComponentModel.Description("The client")] HttpClient http) => http.ToString() ?? "";

    /// <summary>Waits with no way to stop it.</summary>
    [System.ComponentModel.Description("Waits, with no way to stop it.")]
    public Task<string> Wait([System.ComponentModel.Description("What to wait for")] string query) =>
        Task.FromResult(query);

    /// <summary>A name one character too long.</summary>
    [System.ComponentModel.Description("A name one character too long.")]
    public string AToolNameOfSixtyFiveCharactersIsOneMoreThanAProviderAcceptsSafelyXYZ() => "";
}

/// <summary>
/// A tool as a pack's tool should be. Its description gives a web address with the identity's name in it, which is a
/// page's address and not the agent, so it is no problem.
/// </summary>
public sealed class FakeFineTools
{
    /// <summary>Finds, and can be stopped.</summary>
    [System.ComponentModel.Description("Finds, as a pack's tool should, on a page such as /acme/pages.")]
    public Task<string> Find([System.ComponentModel.Description("What to find")] string query,
        CancellationToken cancellationToken = default) => Task.FromResult(query);
}
