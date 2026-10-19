using System.Text.RegularExpressions;

namespace LouisAgent.Puls.Tests;

/// <summary>
/// A component's rendered markup pinned to a golden file beside its test (tests/CLAUDE.md, golden files):
/// <c>LOUIS_UPDATE_GOLDEN=1</c> writes it, any other run compares. Per-instance SVG ids and the seconds a thinking block
/// took are normalised. The same helper as the shared library's tests keep: no test project references another.
/// </summary>
internal static partial class Snapshots
{
    public static string Normalise(string markup)
    {
        string text = markup.ReplaceLineEndings("\n");
        text = InstanceIds().Replace(text, "$1-ID-");
        text = ElementReferences().Replace(text, "blazor:elementReference=\"ID\"");
        text = ThoughtFor().Replace(text, "$1Ns");
        return text.TrimEnd() + "\n";
    }

    /// <param name="file">The golden's path inside this project, beside its test (<c>Components/WorkPanel.html</c>).</param>
    public static void Match(string file, string markup)
    {
        string path = Path.Combine(TestPaths.RepoRoot, "tests", "louis-agent.puls.tests", file.Replace('/', Path.DirectorySeparatorChar));
        string actual = Normalise(markup);
        if (Environment.GetEnvironmentVariable("LOUIS_UPDATE_GOLDEN") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, actual);
            Assert.Pass($"Wrote {path}; review its diff before committing.");
        }

        Assert.That(File.Exists(path), $"No golden file; run with LOUIS_UPDATE_GOLDEN=1 to write {path}.");
        // A mismatch leaves what was rendered beside the temp files, so the diff can be read; the golden stays as it is.
        string rendered = Path.Combine(Path.GetTempPath(), "louis-agent-snapshots", Path.GetFileName(path));
        Directory.CreateDirectory(Path.GetDirectoryName(rendered)!);
        File.WriteAllText(rendered, actual);
        Assert.That(actual, Is.EqualTo(File.ReadAllText(path).ReplaceLineEndings("\n")),
            $"{file}: the component's markup changed. If that is intended, rerun with LOUIS_UPDATE_GOLDEN=1 and review the diff.");
    }

    [GeneratedRegex(@"\b([a-z]+)-[0-9a-f]{8}-")]
    private static partial Regex InstanceIds();

    /// <summary>An element with an <c>@ref</c> carries a fresh GUID on every render.</summary>
    [GeneratedRegex(@"blazor:elementReference=""[0-9a-f-]+""")]
    private static partial Regex ElementReferences();

    [GeneratedRegex(@"(Thought for |Gedink vir )\d+s")]
    private static partial Regex ThoughtFor();
}
