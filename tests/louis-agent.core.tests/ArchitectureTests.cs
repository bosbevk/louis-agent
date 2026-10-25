using System.Text.RegularExpressions;

namespace LouisAgent.Core.Tests;

// docs/CLAUDE.md, "Project layout": core's folders split the code by concern, a file's namespace is its folder path,
// a file holds one top-level type, and dependencies point one way (rule 2). Read from the source files, so it checks
// what a reviewer would read.
public partial class ArchitectureTests
{
    private const string RootNamespace = "LouisAgent.Core";

    // A folder may use its own layer and the ones below; Tools never uses Llm (a tool doesn't call a model).
    private static readonly Dictionary<string, int> Layers = new()
    {
        ["Config"] = 0, ["Messages"] = 0, ["Logging"] = 0,
        ["Guardrails"] = 1,
        ["Llm"] = 2, ["Tools"] = 2, ["Sessions"] = 2, ["Skills"] = 2, ["Usage"] = 2, ["Pricing"] = 2, ["Protocols"] = 2,
        ["Agent"] = 3,
        ["Hosting"] = 4,
    };

    // Today's violations, while CL1 (docs/plans/CL1-project-layout.md) moves the folders one by one: each move removes
    // its entries, and the list is empty when the layout is done. A ratchet: a new violation fails, and so does an entry
    // that no longer violates, so the list can only shrink.
    private static readonly HashSet<string> Pending =
    [
    ];

    [Test]
    public void NoViolations_BeyondThePendingList()
    {
        List<string> unexpected = Violations().Where(v => !Pending.Contains(v)).ToList();

        Assert.That(unexpected, Is.Empty, "Layout violations (docs/CLAUDE.md, Project layout):\n" + string.Join("\n", unexpected));
    }

    [Test]
    public void ThePendingList_OnlyHoldsRealViolations()
    {
        HashSet<string> current = Violations().ToHashSet();
        List<string> fixedAlready = Pending.Where(p => !current.Contains(p)).ToList();

        Assert.That(fixedAlready, Is.Empty, "No longer violations; remove them from Pending:\n" + string.Join("\n", fixedAlready));
    }

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

    // CL13, review of PR #59: inside Skills/, the top (the contract and the embedded files) uses neither subfolder, and
    // Providers/ and Roles/ use the top, never each other; SkillFiles once used RoleCatalog while RoleCatalog read
    // SkillFileSource, a cycle the layers above can't see. Skills only: a factory at a seam's top (Tools/Shops) may
    // create its providers. The one exception is Messages/ (the split by area, F18): every folder of Skills/ may use it, as
    // every folder of core may use core's Messages/, because its enums are the texts, not the code under test.
    [Test]
    public void InsideSkills_TheTopUsesNothingBelow_AndItsFoldersOnlyTheTop()
    {
        string skills = Path.Combine(TestPaths.RepoRoot, "louis-agent.core", "Skills");
        var found = new List<string>();
        foreach (string path in Directory.EnumerateFiles(skills, "*.cs", SearchOption.AllDirectories))
        {
            string file = Path.GetRelativePath(skills, path).Replace('\\', '/');
            string folder = Path.GetDirectoryName(file)!.Replace('\\', '/');
            foreach (string used in SkillsUsing().Matches(File.ReadAllText(path)).Select(u => u.Groups[1].Value).Distinct())
                if (used != folder && used != "Messages") found.Add($"Skills/{file} uses Skills.{used}");
        }

        Assert.That(found, Is.Empty, "Skills/: the top uses nothing below it except Messages/; Providers/ and Roles/ use only the top and Messages/");
    }

    // FX19 (ADR 0004): a process starts from an argument list, never from one command-line string, so a value from the
    // model can't be split into more arguments. A ProcessStartInfo.Arguments assignment is the one way to build such a
    // string; this fails on it anywhere in core, so it can't come back. (Other types have an Arguments property too:
    // a tool call's, a record's; only a file that starts a process is read.)
    [Test]
    public void NoFileInCore_BuildsAProcessCommandLineAsOneString()
    {
        string core = Path.Combine(TestPaths.RepoRoot, "louis-agent.core");
        var found = Directory.EnumerateFiles(core, "*.cs", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(core, f).Replace('\\', '/'))
            .Where(f => !f.StartsWith("bin/") && !f.StartsWith("obj/"))
            .Where(f => File.ReadAllText(Path.Combine(core, f)) is { } source && source.Contains("ProcessStartInfo") && ArgumentsString().IsMatch(source))
            .ToList();

        Assert.That(found, Is.Empty, "Builds a command line as one string (ADR 0004); use ProcessRunner's argument list:\n" + string.Join("\n", found));
    }

    [GeneratedRegex(@"(^|[\s.])Arguments\s*=[^=]", RegexOptions.Multiline)]
    private static partial Regex ArgumentsString();

    // ---- CL15: the cut. Core is the engine and names no identity, pack or product; a pack references no other pack and
    // no identity (ADR 0027); and core's model-visible text names no product, held by a ratchet like Pending's. ----

    [Test]
    public void Core_ReferencesNoIdentityOrPack()
    {
        string core = Path.Combine(TestPaths.RepoRoot, "louis-agent.core");
        Assert.That(ForbiddenReferences(File.ReadAllText(Path.Combine(core, "louis-agent.core.csproj"))), Is.Empty, "core's csproj references an identity or a pack");

        var usings = Directory.EnumerateFiles(core, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(f => OutsideCoreUsing().IsMatch(File.ReadAllText(f)))
            .Select(f => Path.GetRelativePath(core, f))
            .ToList();
        Assert.That(usings, Is.Empty, "core uses an identity's, a pack's or the composition's namespace");
    }

    [Test]
    public void NoPack_ReferencesAnotherPack_OrAnIdentity()
    {
        // The rule on a fixture, read as text (never a loose csproj the build would pick up), then on every pack on disk.
        const string offending = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <ProjectReference Include="..\..\louis-agent.core\louis-agent.core.csproj" />
                <ProjectReference Include="..\louis-agent.pack.recipes\louis-agent.pack.recipes.csproj" />
                <ProjectReference Include="..\..\identities\louis-agent.identity.puls\louis-agent.identity.puls.csproj" />
              </ItemGroup>
            </Project>
            """;
        Assert.That(ForbiddenReferences(offending), Is.EqualTo(new[] { @"..\louis-agent.pack.recipes\louis-agent.pack.recipes.csproj", @"..\..\identities\louis-agent.identity.puls\louis-agent.identity.puls.csproj" }));

        string packs = Path.Combine(TestPaths.RepoRoot, "packs");
        var found = (Directory.Exists(packs) ? Directory.EnumerateFiles(packs, "*.csproj", SearchOption.AllDirectories) : [])
            .SelectMany(f => ForbiddenReferences(File.ReadAllText(f)).Select(r => $"{Path.GetRelativePath(packs, f)}: {r}"))
            .ToList();
        Assert.That(found, Is.Empty, "a pack references another pack or an identity (ADR 0027):\n" + string.Join("\n", found));
    }

    [Test]
    public void CoresSource_NamesNoProduct()
    {
        // Step 5: the ratchet above holds the model-visible text; the comments and examples follow it, so a reader of
        // core never meets a product, a shop or a country. Those live in an identity or a pack, and core's examples
        // use made-up names.
        string core = Path.Combine(TestPaths.RepoRoot, "louis-agent.core");
        var found = Directory.EnumerateFiles(core, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Append(Path.Combine(core, "CLAUDE.md"))
            .SelectMany(f => File.ReadLines(f)
                .Select((line, i) => (Line: line, Number: i + 1))
                .Where(x => ProductName().IsMatch(x.Line))
                .Select(x => $"{Path.GetRelativePath(core, f).Replace('\\', '/')}:{x.Number}: {x.Line.Trim()}"))
            .ToList();
        Assert.That(found, Is.Empty, "core's source names a product, a shop or a country (CL15 step 5):\n" + string.Join("\n", found));
    }

    [Test]
    public void NoTestProject_LeansOnCoresTests()
    {
        // Louis, review of step 4: the fakes the test projects share (FakeChatClient, StubHandler, FakePrompts, TestPaths)
        // live in tests/louis-agent.testing, referenced by the projects that use them; core.tests is referenced by no
        // project and opens its internals to none, so a test project's fakes never become another project's dependency.
        string tests = Path.Combine(TestPaths.RepoRoot, "tests");
        var leaning = Directory.EnumerateFiles(tests, "*.csproj", SearchOption.AllDirectories)
            .Where(f => File.ReadAllText(f).Contains("louis-agent.core.tests.csproj", StringComparison.Ordinal))
            .Select(f => Path.GetRelativePath(tests, f).Replace('\\', '/'))
            .ToList();
        Assert.That(leaning, Is.Empty, "a test project references core.tests; its fakes belong in louis-agent.testing");
        Assert.That(File.ReadAllText(Path.Combine(tests, "louis-agent.core.tests", "louis-agent.core.tests.csproj")), Does.Not.Contain("InternalsVisibleTo"));
    }

    [GeneratedRegex(@"\b(puls|checkers|south\s+africa|johannesburg)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ProductName();

    /// <summary>The project references a csproj makes into <c>packs/</c> or <c>identities/</c> (core's and a pack's may make none).</summary>
    internal static IReadOnlyList<string> ForbiddenReferences(string csproj) =>
        ProjectReference().Matches(csproj).Select(m => m.Groups[1].Value)
            .Where(include => PackOrIdentityPath().IsMatch(include))
            .ToList();

    // What the model reads from core, and may not name: a product (puls), its country, or its Afrikaans briefing labels.
    // Today's pieces, each "file: what", until steps 3 and 4 move them out; a new one fails, a stale entry fails too.
    private static readonly HashSet<string> PendingProductText =
    [
        // Step 3 moved Puls's files to its identity, split its protocol out of the shared guides, and took the Puls
        // names out of ToolMessage and MinionTools and South Africa out of GetToday (20 pieces); step 4 moved the shops,
        // whose area text named Puls, to its pack. Empty since: the cut held.
    ];

    private static readonly (string Label, Regex Pattern)[] ProductWords =
    [
        ("puls", new Regex(@"\bpuls\b", RegexOptions.IgnoreCase)),
        ("South Africa", new Regex(@"South Africa")),
        ("Afrikaans briefing label", new Regex(@"Vir hoeveel|Wil hê|stemming")),
    ];

    [Test]
    public void CoresModelVisibleText_NamesNoProduct_BeyondThePendingList()
    {
        List<string> unexpected = ProductText().Where(v => !PendingProductText.Contains(v)).ToList();
        Assert.That(unexpected, Is.Empty, "Core's model-visible text names a product (CL15):\n" + string.Join("\n", unexpected));
    }

    [Test]
    public void ThePendingProductText_OnlyHoldsRealPieces()
    {
        HashSet<string> current = ProductText().ToHashSet();
        List<string> gone = PendingProductText.Where(p => !current.Contains(p)).ToList();
        Assert.That(gone, Is.Empty, "No longer there; remove them from PendingProductText:\n" + string.Join("\n", gone));
    }

    /// <summary>Every "file: word" in core's embedded prompts and in the [Description] texts of its C# (tool descriptions, ToolMessage members).</summary>
    private static IEnumerable<string> ProductText()
    {
        string core = Path.Combine(TestPaths.RepoRoot, "louis-agent.core");
        var pieces = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(Path.Combine(core, "Prompts"), "*.md", SearchOption.AllDirectories))
            Add(Path.GetRelativePath(core, file), File.ReadAllText(file));
        foreach (string file in Directory.EnumerateFiles(core, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;
            string descriptions = string.Join("\n", DescriptionText().Matches(File.ReadAllText(file)).Select(m => m.Groups[1].Value));
            Add(Path.GetRelativePath(core, file), descriptions);
        }
        return pieces;

        void Add(string file, string text)
        {
            foreach (var (label, pattern) in ProductWords)
                if (pattern.IsMatch(text)) pieces.Add($"{file.Replace('\\', '/')}: {label}");
        }
    }

    [GeneratedRegex(@"<ProjectReference\s+Include=""([^""]+)""")]
    private static partial Regex ProjectReference();

    [GeneratedRegex(@"[\\/](packs|identities)[\\/]|^\.\.[\\/]louis-agent\.(pack|identity)\.")]
    private static partial Regex PackOrIdentityPath();

    [GeneratedRegex(@"^using\s+LouisAgent\.(Identities|Packs|Composition)\b", RegexOptions.Multiline)]
    private static partial Regex OutsideCoreUsing();

    // Everything inside a [Description(…)]: one literal, literals joined with +, or a multi-line raw string.
    [GeneratedRegex(@"\[Description\((.*?)\)\]", RegexOptions.Singleline)]
    private static partial Regex DescriptionText();

    [GeneratedRegex(@"^using\s+LouisAgent\.Core\.Skills\.(\w+)\s*;", RegexOptions.Multiline)]
    private static partial Regex SkillsUsing();

    [GeneratedRegex(@"^namespace\s+([\w.]+)\s*;", RegexOptions.Multiline)]
    private static partial Regex Namespace();

    // Top-level declarations start at column 0 (file-scoped namespaces); nested types are indented.
    [GeneratedRegex(@"^(?:(?:public|internal|file|static|sealed|abstract|partial|readonly)\s+)*(?:class|record|enum|interface|struct)\s+\w",
        RegexOptions.Multiline)]
    private static partial Regex TopLevelType();

    [GeneratedRegex(@"^using\s+LouisAgent\.Core\.(\w+)", RegexOptions.Multiline)]
    private static partial Regex CoreUsing();
}
