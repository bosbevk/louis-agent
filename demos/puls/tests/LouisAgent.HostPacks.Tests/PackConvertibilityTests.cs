using LouisAgent.Composition.Packs;
using LouisAgent.HostPacks;
using LouisAgent.Testing.Packs;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.HostPacks.Tests;

// CL15 step 4 (ADR 0028): every pack the composition root can build stays convertible to an MCP server. The check
// is the shared PackConvertibility (F106.3.2), so the coding catalogue holds its packs the same way. It sees
// shapes, not behaviour: each tool's function is built as the engine builds it (AgentCatalog.ToolMethodsOf and
// FunctionOf, from the tool type, so no setting, browser or network), then wrapped by the MCP SDK's own McpServerTool,
// which must keep its name, its input schema and its description; the tool returns text, takes plain data and a
// CancellationToken when it waits, carries an MCP-safe name, and names no identity; the pack's guides are prose in its
// own prompts. What the test can't see (a thrown failure, input read from the session, framing and bounds) is ADR 0028's
// review checklist.
public class PackConvertibilityTests
{
    private const string NameRule = PackConvertibility.NameRule;

    [TestCaseSource(nameof(EveryPackKind))]
    public void EveryPack_IsConvertibleToAnMcpServer(PackKind kind)
    {
        IAgentPack pack = Plugins.FromSettings(null, kind.ToString()).Packs.Single();
        Assert.That(Problems(pack), Is.Empty);
    }

    private static IEnumerable<PackKind> EveryPackKind() => Enum.GetValues<PackKind>();

    [Test]
    public void AnUnconvertiblePack_ListsExactlyItsProblems()
    {
        Assert.That(Problems(new FakeUnconvertiblePack()), Is.EquivalentTo(new[]
        {
            "fakebad/FindForPuls: its description names the identity 'Puls'",
            "fakebad/FindForPuls: parameter 'area' names the identity 'Puls'",
            "fakebad/Count: returns Int32, not string, Task<string> or ValueTask<string>",
            "fakebad/Read: parameter 'http' is HttpClient, not plain data",
            "fakebad/Wait: waits but takes no CancellationToken",
            "fakebad/AToolNameOfSixtyFiveCharactersIsOneMoreThanAProviderAcceptsSafelyXYZ: its name isn't " + NameRule,
            "fakebad: toolsets/fakebad.md holds a '## Skill:' procedure; a pack's guide is prose, a procedure becomes a tool",
            "fakenoguide: toolsets/fakenoguide.md isn't in the pack's prompts",
        }));
    }

    /// <summary>The shared check, with Puls's identities as the names a tool may not carry.</summary>
    private static IReadOnlyList<string> Problems(IAgentPack pack) =>
        PackConvertibility.Problems(pack, Enum.GetNames<IdentityKind>());
}

/// <summary>A pack that breaks every rule once, so the test's list of problems is checked against a known set.</summary>
internal sealed class FakeUnconvertiblePack : IAgentPack
{
    public string Name => "fakeunconvertible";

    public IReadOnlyList<PackToolset> Toolsets { get; } =
    [
        new("fakebad", "Breaks the tool rules", GuideFile: "toolsets/fakebad.md", MissingSettings: _ => [],
            CreateTools: _ => [new FakeUnconvertibleTools()], ToolTypes: [typeof(FakeUnconvertibleTools)]),
        new("fakenoguide", "Its guide isn't in its prompts", GuideFile: "toolsets/fakenoguide.md", MissingSettings: _ => [],
            CreateTools: _ => [new FakeFineTools()], ToolTypes: [typeof(FakeFineTools)]),
    ];

    public IFileProvider Prompts { get; } = new FakePrompts(new Dictionary<string, string>
    {
        ["toolsets/fakebad.md"] = "# Fakebad\n\nA guide with a procedure.\n\n## Skill: Greet\n\n- Execution:\n\n```bash\necho hi\n```\n",
    });

    public IReadOnlyList<string> Requires => [];
}

public sealed class FakeUnconvertibleTools
{
    [System.ComponentModel.Description("Finds what Puls asked for.")]
    public string FindForPuls([System.ComponentModel.Description("The town, when Puls gave none")] string area) => area;

    [System.ComponentModel.Description("Counts, in a number.")]
    public int Count([System.ComponentModel.Description("What to count")] string query) => query.Length;

    [System.ComponentModel.Description("Reads through a client it is handed.")]
    public string Read([System.ComponentModel.Description("The client")] HttpClient http) => http.ToString() ?? "";

    [System.ComponentModel.Description("Waits, with no way to stop it.")]
    public Task<string> Wait([System.ComponentModel.Description("What to wait for")] string query) => Task.FromResult(query);

    [System.ComponentModel.Description("A name one character too long.")]
    public string AToolNameOfSixtyFiveCharactersIsOneMoreThanAProviderAcceptsSafelyXYZ() => "";
}

public sealed class FakeFineTools
{
    [System.ComponentModel.Description("Finds, as a pack's tool should.")]
    public Task<string> Find([System.ComponentModel.Description("What to find")] string query, CancellationToken cancellationToken = default) => Task.FromResult(query);
}
