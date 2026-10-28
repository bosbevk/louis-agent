using LouisAgent.Core.Tests.RepeatedNames.Scanning;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LouisAgent.Core.Tests.RepeatedNames;

/// <summary>
/// Fails when a name that is an enum member is typed as a plain string anywhere else in the sources, so a typo can't
/// compile and a rename is a rename.
/// </summary>
/// <remarks>
/// A ratchet. It reads the wire names out of the enums listed below (the toolsets, the script languages, the eval
/// sets' names) and scans standalone regular literals only: a name inside a raw, verbatim or interpolated string is
/// wire data (an expected JSON, a message) and stays text. Two things aren't a name of the set and are skipped by what
/// they are: an executable's name (<c>git</c>, <c>dotnet</c>, <c>python</c>) and a key read from JSON or a dictionary
/// (<c>GetProperty("web")</c>). Anything else is listed in Exemptions with its reason; if that list passes a dozen,
/// the scan is too eager and gets narrowed instead.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public class RepeatedNameTests
{
    /// <summary>
    /// The enums whose wire names the scan guards (each the owner of its names); added to as each set of names becomes
    /// an enum.
    /// </summary>
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
        "evals/louis-agent.evals.puls/PulsEvalSet.cs",
        "evals/louis-agent.evals.template/TemplateEvalSet.cs",
        "identities/louis-agent.identity.puls/PulsIdentity.cs",
        "src/louis-agent.composition/IdentityKind.cs",
        "identities/louis-agent.identity.puls/PulsPack.cs",
        "src/louis-agent.composition/PackKind.cs",
        "src/louis-agent.api/Endpoints/ApiErrorKind.cs",
    ];

    /// <summary>
    /// The files that hold a dataset's ids as test constants (ADR 0043: an id is data, not an enum), each with the one
    /// class whose body is skipped. Named here, so a new file of the kind is a decision in this list and not a file
    /// name that hides what is typed in it; the ids themselves are checked against the dataset by the tests of the
    /// project that owns them.
    /// </summary>
    private static readonly (string File, string Class)[] IdsFiles =
    [
        ("tests/louis-agent.evals.puls.tests/PulsTaskIds.cs", "PulsTaskIds"),
        ("tests/louis-agent.evals.puls.tests/PulsCookScenarioIds.cs", "PulsCookScenarioIds"),
    ];

    /// <summary>
    /// A literal that is a name of something else, with the file (or the folder, ending in /) it is in, a text on its
    /// line, and why. Few on purpose (Louis, 2026-10-27: a dozen is too many).
    /// </summary>
    private static readonly (string File, string Line, string Reason)[] Exemptions =
    [
        ("src/louis-agent.puls/Components/Landing/", "",
            "the landing page's display content: a mock of a chat, with the toolsets it pretends to use"),
        ("tests/louis-agent.evals.tests/Protocol/ProtocolWriterTests.cs", "",
            "the writer's lines are compared with the Python-recorded fixture, which holds these names: "
            + "recorded data stays as recorded"),
        ("tests/louis-agent.web.testing/SessionStreamFixture.cs", "CookRole",
            "the role of the recorded turn, named once: the goldens and the replay tests read it"),
        ("tests/louis-agent.api.tests/Evals/Reports/EvalManualTests.cs", "",
            "each real eval beside its manual: the names the real runner offers"),
        ("tests/louis-agent.api.tests/Sessions/StreamGoldenTests.cs", "call_sub",
            "the sub-session event is written into the recorded stream golden, which holds the peer's name: "
            + "recorded data stays as recorded"),
        ("tests/louis-agent.api.tests/Evals/Runs/Protocol/EvalProtocolTests.cs", "api_error",
            "a turn's failure (EvalTurnFailure), not the API's error kind that shares its text: "
            + "the line read as the runner writes it"),
        ("tests/louis-agent.api.tests/Evals/Store/Recording/EvalRecorderTests.cs", "api_error",
            "a turn's failure (EvalTurnFailure), not the API's error kind that shares its text: "
            + "the runner's line as recorded"),
    ];

    /// <summary>Every source file the scan looks at.</summary>
    private static IEnumerable<string> SourceFiles() => SourceTree.Files();

    /// <summary>A file's path from the repo's root, with forward slashes.</summary>
    private static string Relative(string path) => SourceTree.Relative(path);

    /// <summary>
    /// The wire names an enum file declares: a member's <c>[WireName]</c> (or .NET's <c>[JsonStringEnumMemberName]</c>
    /// under it), or its name in lower case.
    /// </summary>
    private static IEnumerable<string> WireNames(string file) =>
        CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(TestPaths.RepoRoot, file))).GetRoot()
            .DescendantNodes().OfType<EnumMemberDeclarationSyntax>()
            .Select(member => WireNameOf(member) ?? member.Identifier.ValueText.ToLowerInvariant());

    /// <summary>
    /// The text in a member's <c>[WireName]</c> (or <c>[JsonStringEnumMemberName]</c>), or null when it has neither.
    /// </summary>
    private static string? WireNameOf(EnumMemberDeclarationSyntax member) =>
        member.AttributeLists.SelectMany(list => list.Attributes)
            .Where(attribute => attribute.Name.ToString() is "WireName" or "JsonStringEnumMemberName")
            .Select(attribute =>
                (attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression as LiteralExpressionSyntax)?.Token
                .ValueText)
            .FirstOrDefault();

    /// <summary>A name spelt as a string in a file: where, which name, and the line's text.</summary>
    private sealed record Site(string File, int Line, string Name, string Text);

    /// <summary>Every place in the sources that spells one of the names as a string.</summary>
    private static List<Site> Sites()
    {
        var names = EnumFiles.SelectMany(WireNames).ToHashSet(StringComparer.Ordinal);
        var sites = new List<Site>();
        foreach (string path in SourceFiles())
        {
            string rel = Relative(path);
            // An enum file is scanned like any other: only an enum's own body is skipped. The same holds for the one
            // class of an ids file.
            if (rel.StartsWith("tests/louis-agent.core.tests/RepeatedNames/")) continue;
            var idsClasses = IdsFiles.Where(ids => ids.File == rel).Select(ids => ids.Class).ToList();
            // A razor file is markup around C#, so it can't be parsed as C#: it has its own scan.
            var hits = rel.EndsWith(".razor", StringComparison.Ordinal)
                ? RazorNameScan.Flagged(File.ReadAllText(path), names)
                : NameScan.Flagged(File.ReadAllText(path), names, idsClasses);
            sites.AddRange(hits.Select(hit => new Site(rel, hit.Line, hit.Value, hit.Text)));
        }

        return sites;
    }

    /// <summary>Whether a file is the exempt file, or inside the exempt folder.</summary>
    private static bool InFile(string file, string exempt) =>
        exempt.EndsWith('/') ? file.StartsWith(exempt, StringComparison.Ordinal) : file == exempt;

    /// <summary>Whether the string is one of the few the list lets through, with its reason.</summary>
    private static bool IsExempt(Site site) => Exemptions.Any(e =>
        InFile(site.File, e.File) && site.Text.Contains(e.Line, StringComparison.Ordinal));

    /// <summary>Every enum file gives some names, and the toolsets and the languages are among them.</summary>
    [Test]
    public void TheEnumFilesDeclareTheirNames()
    {
        foreach (string file in EnumFiles)
            Assert.That(WireNames(file).ToList(), Is.Not.Empty, file);
        Assert.That(EnumFiles.SelectMany(WireNames), Does.Contain("web").And.Contain("shops").And.Contain("powershell"),
            "the scan guards the toolsets and the languages");
    }

    /// <summary>A name that is an enum member is never typed as a string outside its enum.</summary>
    [Test]
    public void NoWireNameIsWrittenAsAString_OutsideItsEnum()
    {
        var offending = Sites().Where(s => !IsExempt(s)).Select(s => $"{s.File}:{s.Line}  \"{s.Name}\"  {s.Text}")
            .ToList();

        Assert.That(offending, Is.Empty,
            "a name that repeats is an enum member (ADR 0043):\n" + string.Join("\n", offending.Take(150)) +
            $"\n({offending.Count} in all)");
    }

    /// <summary>A string on the exemption list still exists, so the list can't hold names that are gone.</summary>
    [Test]
    public void EveryExemption_StillMatchesALiteral_SoTheListCannotGoStale()
    {
        var sites = Sites();
        var stale = Exemptions
            .Where(e => !sites.Any(s => InFile(s.File, e.File) && s.Text.Contains(e.Line, StringComparison.Ordinal)))
            .Select(e => $"{e.File}: {e.Line}").ToList();

        Assert.That(stale, Is.Empty);
    }

    /// <summary>The list stays short, because a long one means the scan is too eager.</summary>
    [Test]
    public void TheExemptions_StayUnderADozen_OtherwiseTheScanIsTooEager()
    {
        Assert.That(Exemptions.Length, Is.LessThanOrEqualTo(12), "narrow the scan instead of listing more");
        Assert.That(Exemptions.Select(e => e.Reason), Has.All.Not.Empty);
    }
}
