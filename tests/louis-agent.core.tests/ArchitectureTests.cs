using System.Text.RegularExpressions;

namespace LouisAgent.Core.Tests;

/// <summary>
/// Keeps the shape of the code the way the docs promise: core split into folders by concern, a namespace per folder,
/// one type per file, dependencies one way, and the engine, the packs, the identities and the check contract each
/// referencing only what they may. It reads the source files, so it checks what a reviewer would read.
/// </summary>
/// <remarks>
/// Architecture tests as a fence: a rule held by a test can't drift the way a rule held only by a doc does.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0010-folders-by-concern.md">ADR 0010: folders by concern</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0027-engine-identities-and-packs.md">ADR 0027: engine, identities and packs</seealso>
public partial class ArchitectureTests
{
    private const string RootNamespace = "LouisAgent.Core";

    // A folder may use its own layer and the ones below; Tools never uses Llm (a tool doesn't call a model).
    private static readonly Dictionary<string, int> Layers = new()
    {
        ["Config"] = 0,
        ["Messages"] = 0,
        ["Logging"] = 0,
        ["Guardrails"] = 1,
        ["Llm"] = 2,
        ["Tools"] = 2,
        ["Sessions"] = 2,
        ["Skills"] = 2,
        ["Usage"] = 2,
        ["Pricing"] = 2,
        ["Protocols"] = 2,
        ["Agent"] = 3,
        ["Hosting"] = 4,
    };

    // The layout's known violations, a ratchet: a new violation fails, and so does an entry that no longer violates,
    // so the list can only shrink. Empty since CL1 moved the last folder.
    private static readonly HashSet<string> Pending =
    [
    ];

    /// <summary>Core's layout holds: no file breaks it unless it's on the known list.</summary>
    [Test]
    public void NoViolations_BeyondThePendingList()
    {
        List<string> unexpected = Violations().Where(v => !Pending.Contains(v)).ToList();

        Assert.That(unexpected, Is.Empty,
            "Layout violations (docs/CLAUDE.md, Project layout):\n" + string.Join("\n", unexpected));
    }

    /// <summary>A fixed violation leaves the known list, so the list only ever shrinks.</summary>
    [Test]
    public void ThePendingList_OnlyHoldsRealViolations()
    {
        HashSet<string> current = Violations().ToHashSet();
        List<string> fixedAlready = Pending.Where(p => !current.Contains(p)).ToList();

        Assert.That(fixedAlready, Is.Empty,
            "No longer violations; remove them from Pending:\n" + string.Join("\n", fixedAlready));
    }

    /// <summary>
    /// Walks every file in core and says what's out of place: the wrong namespace, two types in one file, a folder
    /// that isn't a concern, or a folder that uses one above it.
    /// </summary>
    private static IEnumerable<string> Violations()
    {
        string core = Path.Combine(TestPaths.RepoRoot, "louis-agent.core");
        IEnumerable<string> files = Directory.EnumerateFiles(core, "*.cs", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(core, f).Replace('\\', '/'))
            .Where(f => !f.StartsWith("bin/") && !f.StartsWith("obj/"))
            .Order(StringComparer.Ordinal);

        foreach (string file in files)
        {
            string source = File.ReadAllText(Path.Combine(core, file));
            string folder = Path.GetDirectoryName(file)!.Replace('\\', '/');
            string concern = folder.Split('/')[0];

            string expected = folder.Length == 0 ? RootNamespace : $"{RootNamespace}.{folder.Replace('/', '.')}";
            string? actual = Namespace().Match(source) is { Success: true } m ? m.Groups[1].Value : null;
            if (actual != expected) yield return $"{file}: namespace {actual ?? "(none)"}, expected {expected}";

            int types = TopLevelType().Count(source);
            if (types > 1) yield return $"{file}: {types} top-level types, one per file";

            if (!Layers.TryGetValue(concern, out int layer))
            {
                yield return $"{file}: '{(folder.Length == 0 ? "(root)" : concern)}' isn't a concern folder";
                continue;
            }

            foreach (string used in CoreUsing().Matches(source).Select(u => u.Groups[1].Value).Distinct())
            {
                if (Layers.TryGetValue(used, out int usedLayer) && usedLayer > layer)
                    yield return $"{file}: {concern} (layer {layer}) uses {used} (layer {usedLayer})";
                if (concern == "Tools" && used is "Llm" or "Agent")
                    yield return $"{file}: Tools uses {used}";
            }
        }
    }

    /// <summary>
    /// Inside Skills/, the top never uses its subfolders, and Providers/ and Roles/ use only the top, never each other,
    /// so no cycle hides under the layers.
    /// </summary>
    /// <remarks>
    /// SkillFiles once used RoleCatalog while RoleCatalog read SkillFileSource, a cycle the layer rule can't see
    /// (CL13). Two folders every part may use: Messages/, because its enums are texts, not code under test, and
    /// Languages/, a leaf that uses nothing of core (the script languages a fence can name). This is for Skills only:
    /// elsewhere a factory at a seam's top (Tools/Shops) may create its providers.
    /// </remarks>
    [Test]
    public void InsideSkills_TheTopUsesNothingBelow_AndItsFoldersOnlyTheTop()
    {
        string skills = Path.Combine(TestPaths.RepoRoot, "louis-agent.core", "Skills");
        var found = new List<string>();
        foreach (string path in Directory.EnumerateFiles(skills, "*.cs", SearchOption.AllDirectories))
        {
            string file = Path.GetRelativePath(skills, path).Replace('\\', '/');
            string folder = Path.GetDirectoryName(file)!.Replace('\\', '/');
            foreach (string used in SkillsUsing().Matches(File.ReadAllText(path)).Select(u => u.Groups[1].Value)
                         .Distinct())
                if (used != folder && used is not ("Messages" or "Languages"))
                    found.Add($"Skills/{file} uses Skills.{used}");
        }

        Assert.That(found, Is.Empty,
            "Skills/: the top uses nothing below it except Messages/ and Languages/; " +
            "Providers/ and Roles/ use only the top, Messages/ and Languages/");
    }

    /// <summary>
    /// Core starts a process from a list of arguments, never one command-line string, so a value from the model can't
    /// be split into extra arguments.
    /// </summary>
    /// <remarks>
    /// Setting ProcessStartInfo.Arguments is the one way to build such a string, so this fails on it anywhere in core
    /// (FX19). Other types have an Arguments property too, so only a file that starts a process is read.
    /// </remarks>
    /// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
    [Test]
    public void NoFileInCore_BuildsAProcessCommandLineAsOneString()
    {
        string core = Path.Combine(TestPaths.RepoRoot, "louis-agent.core");
        var found = Directory.EnumerateFiles(core, "*.cs", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(core, f).Replace('\\', '/'))
            .Where(f => !f.StartsWith("bin/") && !f.StartsWith("obj/"))
            .Where(f => File.ReadAllText(Path.Combine(core, f)) is { } source && source.Contains("ProcessStartInfo") &&
                        ArgumentsString().IsMatch(source))
            .ToList();

        Assert.That(found, Is.Empty,
            "Builds a command line as one string (ADR 0004); use ProcessRunner's argument list:\n" +
            string.Join("\n", found));
    }

    /// <summary>Finds an assignment to an Arguments property (not a comparison).</summary>
    [GeneratedRegex(@"(^|[\s.])Arguments\s*=[^=]", RegexOptions.Multiline)]
    private static partial Regex ArgumentsString();

    // ---- CL15: the cut. Core is the engine and names no identity, pack or product; a pack references no other pack
    // and no identity (ADR 0027); and core's model-visible text names no product, held by a ratchet like
    // Pending's. ----

    /// <summary>
    /// The engine never reaches for an identity, a pack or the composition, by reference or by namespace.
    /// </summary>
    [Test]
    public void Core_ReferencesNoIdentityOrPack()
    {
        string core = Path.Combine(TestPaths.RepoRoot, "louis-agent.core");
        Assert.That(ForbiddenReferences(File.ReadAllText(Path.Combine(core, "louis-agent.core.csproj"))), Is.Empty,
            "core's csproj references an identity or a pack");

        var usings = Directory.EnumerateFiles(core, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                        !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(f => OutsideCoreUsing().IsMatch(File.ReadAllText(f)))
            .Select(f => Path.GetRelativePath(core, f))
            .ToList();
        Assert.That(usings, Is.Empty, "core uses an identity's, a pack's or the composition's namespace");
    }

    /// <summary>A pack stands on its own: it never references another pack or an identity.</summary>
    [Test]
    public void NoPack_ReferencesAnotherPack_OrAnIdentity()
    {
        // The rule on a fixture, read as text (never a loose csproj the build would pick up), then on every pack on
        // disk.
        const string offending = """
                                 <Project Sdk="Microsoft.NET.Sdk">
                                   <ItemGroup>
                                     <ProjectReference Include="..\..\louis-agent.core\louis-agent.core.csproj" />
                                     <ProjectReference Include="..\louis-agent.pack.recipes\louis-agent.pack.recipes.csproj" />
                                     <ProjectReference Include="..\..\identities\louis-agent.identity.puls\louis-agent.identity.puls.csproj" />
                                   </ItemGroup>
                                 </Project>
                                 """;
        Assert.That(ForbiddenReferences(offending),
            Is.EqualTo(new[]
            {
                @"..\louis-agent.pack.recipes\louis-agent.pack.recipes.csproj",
                @"..\..\identities\louis-agent.identity.puls\louis-agent.identity.puls.csproj"
            }));

        string packs = Path.Combine(TestPaths.RepoRoot, "packs");
        var found = (Directory.Exists(packs)
                ? Directory.EnumerateFiles(packs, "*.csproj", SearchOption.AllDirectories)
                : [])
            .SelectMany(f =>
                ForbiddenReferences(File.ReadAllText(f)).Select(r => $"{Path.GetRelativePath(packs, f)}: {r}"))
            .ToList();
        Assert.That(found, Is.Empty,
            "a pack references another pack or an identity (ADR 0027):\n" + string.Join("\n", found));
    }

    /// <summary>
    /// The check contract may lean only on what a pack leans on already: the engine, the session stream and the eval
    /// protocol. That's why a pack can ship its own check, and why the contract never reaches back into the harness,
    /// a pack or an identity.
    /// </summary>
    /// <remarks>
    /// Separated Interface (Fowler, PoEAA): the contract sits in a small project its clients can reference, and the
    /// checks live elsewhere. The references are the whole fence, so this test holds them by name.
    /// </remarks>
    /// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0027-engine-identities-and-packs.md">ADR 0027: engine, identities and packs</seealso>
    /// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
    [Test]
    public void Checks_ReferencesOnlyTheEngineTheProtocolAndTheStream()
    {
        string csproj = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "evals", "louis-agent.evals.checks",
            "louis-agent.evals.checks.csproj"));

        // By file name, so the test reads the same on Linux, where a backslash isn't a folder separator.
        var references = ProjectReference().Matches(csproj)
            .Select(m => Path.GetFileName(m.Groups[1].Value.Replace('\\', '/')));

        Assert.That(references,
            Is.EquivalentTo(new[]
            {
                "louis-agent.core.csproj", "louis-agent.session-stream.csproj", "louis-agent.evals.protocol.csproj"
            }));
        Assert.That(ForbiddenReferences(csproj), Is.Empty);
    }

    /// <summary>
    /// Someone reading core never meets a product, a shop or a country, not even in a comment or an example: those
    /// live in an identity or a pack, and core's examples use made-up names.
    /// </summary>
    [Test]
    public void CoresSource_NamesNoProduct()
    {
        string core = Path.Combine(TestPaths.RepoRoot, "louis-agent.core");
        var found = Directory.EnumerateFiles(core, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                        !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Append(Path.Combine(core, "CLAUDE.md"))
            .SelectMany(f => File.ReadLines(f)
                .Select((line, i) => (Line: line, Number: i + 1))
                .Where(x => ProductName().IsMatch(x.Line))
                .Select(x => $"{Path.GetRelativePath(core, f).Replace('\\', '/')}:{x.Number}: {x.Line.Trim()}"))
            .ToList();
        Assert.That(found, Is.Empty,
            "core's source names a product, a shop or a country (CL15 step 5):\n" + string.Join("\n", found));
    }

    /// <summary>
    /// The shared fakes live in tests/louis-agent.testing, so no test project leans on core's tests, and core's tests
    /// open their internals to nobody.
    /// </summary>
    /// <remarks>
    /// Without it, one test project's fakes (FakeChatClient, StubHandler, FakePrompts, TestPaths) quietly become
    /// another project's dependency.
    /// </remarks>
    [Test]
    public void NoTestProject_LeansOnCoresTests()
    {
        string tests = Path.Combine(TestPaths.RepoRoot, "tests");
        var leaning = Directory.EnumerateFiles(tests, "*.csproj", SearchOption.AllDirectories)
            .Where(f => File.ReadAllText(f).Contains("louis-agent.core.tests.csproj", StringComparison.Ordinal))
            .Select(f => Path.GetRelativePath(tests, f).Replace('\\', '/'))
            .ToList();
        Assert.That(leaning, Is.Empty, "a test project references core.tests; its fakes belong in louis-agent.testing");
        Assert.That(File.ReadAllText(Path.Combine(tests, "louis-agent.core.tests", "louis-agent.core.tests.csproj")),
            Does.Not.Contain("InternalsVisibleTo"));
    }

    /// <summary>A product, a shop or a country that core must never name.</summary>
    [GeneratedRegex(@"\b(puls|checkers|south\s+africa|johannesburg)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ProductName();

    /// <summary>
    /// The references a csproj makes into <c>packs/</c> or <c>identities/</c>; core, a pack and the check contract
    /// may make none.
    /// </summary>
    internal static IReadOnlyList<string> ForbiddenReferences(string csproj) =>
        ProjectReference().Matches(csproj).Select(m => m.Groups[1].Value)
            .Where(include => PackOrIdentityPath().IsMatch(include))
            .ToList();

    // What the model reads from core, and may not name: a product (puls), its country, or its Afrikaans briefing
    // labels. The known pieces, each "file: what"; a new one fails, a stale entry fails too.
    private static readonly HashSet<string> PendingProductText =
    [
        // CL15 step 3 moved Puls's files to its identity, split its protocol out of the shared guides, and took the
        // Puls names out of ToolMessage and MinionTools and South Africa out of GetToday (20 pieces); step 4 moved the
        // shops, whose area text named Puls, to its pack. Empty since: the cut held.
    ];

    private static readonly (string Label, Regex Pattern)[] ProductWords =
    [
        ("Puls", new Regex(@"\bpuls\b", RegexOptions.IgnoreCase)),
        ("South Africa", new Regex(@"South Africa")),
        ("Afrikaans briefing label", new Regex(@"Vir hoeveel|Wil hê|stemming")),
    ];

    /// <summary>What the model reads from core names no product, unless it's on the known list.</summary>
    [Test]
    public void CoresModelVisibleText_NamesNoProduct_BeyondThePendingList()
    {
        List<string> unexpected = ProductText().Where(v => !PendingProductText.Contains(v)).ToList();
        Assert.That(unexpected, Is.Empty,
            "Core's model-visible text names a product (CL15):\n" + string.Join("\n", unexpected));
    }

    /// <summary>A piece that's gone leaves the known list, so that list only shrinks too.</summary>
    [Test]
    public void ThePendingProductText_OnlyHoldsRealPieces()
    {
        HashSet<string> current = ProductText().ToHashSet();
        List<string> gone = PendingProductText.Where(p => !current.Contains(p)).ToList();
        Assert.That(gone, Is.Empty,
            "No longer there; remove them from PendingProductText:\n" + string.Join("\n", gone));
    }

    /// <summary>
    /// Every "file: word" where a product shows up in what the model reads: core's embedded prompts and the
    /// [Description] texts of its C# (tool descriptions, ToolMessage members).
    /// </summary>
    private static IEnumerable<string> ProductText()
    {
        string core = Path.Combine(TestPaths.RepoRoot, "louis-agent.core");
        var pieces = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(Path.Combine(core, "Prompts"), "*.md",
                     SearchOption.AllDirectories))
            Add(Path.GetRelativePath(core, file), File.ReadAllText(file));
        foreach (string file in Directory.EnumerateFiles(core, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") ||
                file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;
            string descriptions = string.Join("\n",
                DescriptionText().Matches(File.ReadAllText(file)).Select(m => m.Groups[1].Value));
            Add(Path.GetRelativePath(core, file), descriptions);
        }

        return pieces;

        // Notes each product word this text names, against its file.
        void Add(string file, string text)
        {
            foreach (var (label, pattern) in ProductWords)
                if (pattern.IsMatch(text))
                    pieces.Add($"{file.Replace('\\', '/')}: {label}");
        }
    }

    /// <summary>The path a csproj's ProjectReference points at.</summary>
    [GeneratedRegex(@"<ProjectReference\s+Include=""([^""]+)""")]
    private static partial Regex ProjectReference();

    /// <summary>A path into <c>packs/</c> or <c>identities/</c>, or to a sibling pack or identity.</summary>
    [GeneratedRegex(@"[\\/](packs|identities)[\\/]|^\.\.[\\/]louis-agent\.(pack|identity)\.")]
    private static partial Regex PackOrIdentityPath();

    /// <summary>A using of an identity's, a pack's or the composition's namespace.</summary>
    [GeneratedRegex(@"^using\s+LouisAgent\.(Identities|Packs|Composition)\b", RegexOptions.Multiline)]
    private static partial Regex OutsideCoreUsing();

    /// <summary>
    /// Everything inside a [Description(…)]: one literal, literals joined with +, or a multi-line raw string.
    /// </summary>
    [GeneratedRegex(@"\[Description\((.*?)\)\]", RegexOptions.Singleline)]
    private static partial Regex DescriptionText();

    /// <summary>The Skills/ subfolder a using reaches into.</summary>
    [GeneratedRegex(@"^using\s+LouisAgent\.Core\.Skills\.(\w+)\s*;", RegexOptions.Multiline)]
    private static partial Regex SkillsUsing();

    /// <summary>A file's file-scoped namespace.</summary>
    [GeneratedRegex(@"^namespace\s+([\w.]+)\s*;", RegexOptions.Multiline)]
    private static partial Regex Namespace();

    /// <summary>
    /// A top-level type: it starts at column 0 (file-scoped namespaces), while nested types are indented.
    /// </summary>
    [GeneratedRegex(
        @"^(?:(?:public|internal|file|static|sealed|abstract|partial|readonly)\s+)*(?:class|record|enum|interface|struct)\s+\w",
        RegexOptions.Multiline)]
    private static partial Regex TopLevelType();

    /// <summary>The core folder a using reaches into.</summary>
    [GeneratedRegex(@"^using\s+LouisAgent\.Core\.(\w+)", RegexOptions.Multiline)]
    private static partial Regex CoreUsing();
}
