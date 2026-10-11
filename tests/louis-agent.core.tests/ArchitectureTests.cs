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
        ["Llm"] = 2, ["Tools"] = 2, ["Skills"] = 2, ["Usage"] = 2, ["Pricing"] = 2, ["Protocols"] = 2,
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

    [GeneratedRegex(@"^namespace\s+([\w.]+)\s*;", RegexOptions.Multiline)]
    private static partial Regex Namespace();

    // Top-level declarations start at column 0 (file-scoped namespaces); nested types are indented.
    [GeneratedRegex(@"^(?:(?:public|internal|file|static|sealed|abstract|partial|readonly)\s+)*(?:class|record|enum|interface|struct)\s+\w",
        RegexOptions.Multiline)]
    private static partial Regex TopLevelType();

    [GeneratedRegex(@"^using\s+LouisAgent\.Core\.(\w+)", RegexOptions.Multiline)]
    private static partial Regex CoreUsing();
}
