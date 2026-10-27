using LouisAgent.Core.Tests.RepeatedNames.Scanning;
using System.Text.RegularExpressions;

namespace LouisAgent.Core.Tests.RepeatedNames;

// F18 step 7.5.3, ADR 0043: a name that repeats is an enum. This ratchet reads the wire names out of the enums of the toolsets and the script languages (the files listed below) and fails on any of
// them written as a plain string literal anywhere else in the sources and tests, so a typo cannot compile and a rename is a rename. It scans standalone regular literals only: a name inside a raw, verbatim
// or interpolated string is wire data (an expected JSON, a message) and stays text. Two things are not a toolset or a language and are skipped by what they are: an executable's name (`git`, `dotnet`, `python`)
// and a key read from JSON or a dictionary (`GetProperty("web")`). Anything else is listed in Exemptions with its reason; if that list passes a dozen, the scan is too eager and gets narrowed instead.
public class RepeatedNameTests
{
    /// <summary>The enums whose wire names the scan guards (each the owner of its names); added to as each set of names becomes an enum.</summary>
    private static readonly string[] EnumFiles =
    [
        "louis-agent.core/Agent/Toolsets/CoreToolset.cs",
        "louis-agent.core/Skills/Languages/ScriptLanguage.cs",
        "packs/louis-agent.pack.shops/ShopsToolset.cs",
        "evals/louis-agent.evals.puls/Shared/PulsToolset.cs",
        "evals/louis-agent.evals.runner/Evals/Stub/StubToolset.cs",
        "src/louis-agent.web.shared/Api/AskedToolset.cs",
        "src/louis-agent.web.shared/Api/ScriptLanguage.cs",
        "evals/louis-agent.evals.puls/Evals/Cook/CookMode.cs",
        "evals/louis-agent.evals.puls/Shared/PulsEvalName.cs",
        "evals/louis-agent.evals.puls/Shared/PulsEvalRole.cs",
        "evals/louis-agent.evals.runner/Evals/Stub/StubMode.cs",
        "evals/louis-agent.evals.runner/Evals/Stub/StubRole.cs",
        "identities/louis-agent.identity.puls/PulsRole.cs",
        "src/louis-agent.puls/Text/PulsWebRole.cs",
        "evals/louis-agent.evals.template/Evals/Example/TemplateEvalName.cs",
    ];

    /// <summary>A literal that is a name of something else, with the file (or the folder, ending in /) it is in, a text on its line, and why. Few on purpose (Louis, 2026-10-27: a dozen is too many).</summary>
    private static readonly (string File, string Line, string Reason)[] Exemptions =
    [
        ("src/louis-agent.puls/Components/Landing/", "", "the landing page's display content: a mock of a chat, with the toolsets it pretends to use"),
        ("tests/louis-agent.evals.tests/Protocol/ProtocolWriterTests.cs", "", "the writer's lines are compared with the Python-recorded fixture, which holds these names: recorded data stays as recorded"),
        ("tests/louis-agent.web.testing/SessionStreamFixture.cs", "CookRole", "the role of the recorded turn, named once: the goldens and the replay tests read it"),
        ("tests/louis-agent.api.tests/Benchmarks/Reports/BenchmarkManualTests.cs", "", "each real eval beside its manual: the names the real runner offers"),
        ("identities/louis-agent.identity.puls/PulsIdentity.cs", "Packs =>", "a pack's name, not a toolset's: the packs set makes it an enum"),
        ("tests/louis-agent.identity.puls.tests/PulsIdentityTests.cs", "puls.Packs", "a pack's name, not a toolset's: the packs set makes it an enum"),
        ("tests/louis-agent.evals.puls.tests/PulsDatasetsTests.cs", "\"numbers\"", "the routing tasks' ids, which a later set (the task ids) makes an enum"),
        ("tests/louis-agent.evals.puls.tests/Evals/Routing/RoutingEvalTests.cs", "\"numbers\"", "the routing tasks' ids, which a later set (the task ids) makes an enum"),
        ("tests/louis-agent.evals.puls.tests/Evals/Routing/Scoring/RoutingScorerTests.cs", "\"numbers\"", "the routing tasks' ids, which a later set (the task ids) makes an enum"),
        ("tests/louis-agent.evals.puls.tests/Evals/Routing/Scoring/RoutingScorerTests.cs", "Dev.Route(\"web\")", "a routing task's id, which a later set (the task ids) makes an enum"),
    ];

    private static IEnumerable<string> SourceFiles() => SourceTree.Files();

    private static string Relative(string path) => SourceTree.Relative(path);

    /// <summary>The wire names an enum file declares: a member's <c>[WireName]</c> (or .NET's <c>[JsonStringEnumMemberName]</c> under it), or its name in lower case.</summary>
    private static IEnumerable<string> WireNames(string file) =>
        Regex.Matches(File.ReadAllText(Path.Combine(TestPaths.RepoRoot, file)), @"^\s*(?:\[(?:WireName|JsonStringEnumMemberName)\(""(?<wire>[^""]+)""\)\]\s*)?(?<name>[A-Z]\w*),\s*$", RegexOptions.Multiline)
            .Select(m => m.Groups["wire"].Success ? m.Groups["wire"].Value : m.Groups["name"].Value.ToLowerInvariant());

    private sealed record Site(string File, int Line, string Name, string Text);

    private static List<Site> Sites()
    {
        var names = EnumFiles.SelectMany(WireNames).ToHashSet(StringComparer.Ordinal);
        var enumFiles = EnumFiles.ToHashSet();
        var sites = new List<Site>();
        foreach (string path in SourceFiles())
        {
            string rel = Relative(path);
            if (enumFiles.Contains(rel) || rel.StartsWith("tests/louis-agent.core.tests/RepeatedNames/")) continue;
            foreach (var hit in NameScan.Flagged(File.ReadAllText(path), names)) sites.Add(new Site(rel, hit.Line, hit.Value, hit.Text));
        }
        return sites;
    }

    private static bool InFile(string file, string exempt) => exempt.EndsWith('/') ? file.StartsWith(exempt, StringComparison.Ordinal) : file == exempt;

    private static bool IsExempt(Site site) => Exemptions.Any(e => InFile(site.File, e.File) && site.Text.Contains(e.Line, StringComparison.Ordinal));

    [Test]
    public void TheEnumFilesDeclareTheirNames()
    {
        foreach (string file in EnumFiles)
            Assert.That(WireNames(file).ToList(), Is.Not.Empty, file);
        Assert.That(EnumFiles.SelectMany(WireNames), Does.Contain("web").And.Contain("shops").And.Contain("powershell"), "the scan guards the toolsets and the languages");
    }

    [Test]
    public void NoWireNameIsWrittenAsAString_OutsideItsEnum()
    {
        var offending = Sites().Where(s => !IsExempt(s)).Select(s => $"{s.File}:{s.Line}  \"{s.Name}\"  {s.Text}").ToList();

        Assert.That(offending, Is.Empty, "a name that repeats is an enum member (ADR 0043):\n" + string.Join("\n", offending.Take(150)) + $"\n({offending.Count} in all)");
    }

    [Test]
    public void EveryExemption_StillMatchesALiteral_SoTheListCannotGoStale()
    {
        var sites = Sites();
        var stale = Exemptions.Where(e => !sites.Any(s => InFile(s.File, e.File) && s.Text.Contains(e.Line, StringComparison.Ordinal))).Select(e => $"{e.File}: {e.Line}").ToList();

        Assert.That(stale, Is.Empty);
    }

    [Test]
    public void TheExemptions_StayUnderADozen_OtherwiseTheScanIsTooEager()
    {
        Assert.That(Exemptions.Length, Is.LessThanOrEqualTo(12), "narrow the scan instead of listing more");
        Assert.That(Exemptions.Select(e => e.Reason), Has.All.Not.Empty);
    }
}
