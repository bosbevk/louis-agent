using System.Text.RegularExpressions;
using LouisAgent.Testing;
using NUnit.Framework;

namespace LouisAgent.Web.Testing;

/// <summary>
/// A component's rendered markup pinned to a golden file beside its test, the way the model's request and the tool
/// texts are pinned (src/tests/CLAUDE.md, golden files): <c>LOUIS_UPDATE_GOLDEN=1</c> writes it, any other run
/// compares, and a mismatch leaves what was rendered under the temp folder's <c>louis-agent-snapshots/</c> so the
/// diff can be read. Normalised, since none is the component's doing: per-instance SVG ids, <c>@ref</c> element references, the
/// renderer's event-handler numbers, the seconds a thinking block took, the scope attribute of a component with its
/// own styles (CL58), and the line breaks beside tags (a page split into components keeps its markup but not its
/// indentation).
/// </summary>
public static partial class Snapshots
{
    public static string Normalise(string markup)
    {
        string text = markup.ReplaceLineEndings("\n");
        text = InstanceIds().Replace(text, "$1-ID-");
        text = ElementReferences().Replace(text, "blazor:elementReference=\"ID\"");
        text = ThoughtFor().Replace(text, "$1Ns");
        text = EventHandlers().Replace(text, "$1=\"N\"");
        text = ScopeAttributes().Replace(text, "b-SCOPE");
        text = LineBreaksAtTags().Replace(text, "");
        return text.TrimEnd() + "\n";
    }

    /// <param name="file">
    /// The golden's path in the running test project, beside its test (<c>Components/TurnView.html</c>).
    /// </param>
    public static void Match(string file, string markup)
    {
        // The running test's own project, three folders above its build output (bin/{configuration}/{framework}).
        string project = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", ".."));
        string path = Path.Combine(project, file.Replace('/', Path.DirectorySeparatorChar));
        string actual = Normalise(markup);
        if (Environment.GetEnvironmentVariable("LOUIS_UPDATE_GOLDEN") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, actual);
            Assert.Pass($"Wrote {path}; review its diff before committing.");
        }

        Assert.That(File.Exists(path), $"No golden file; run with LOUIS_UPDATE_GOLDEN=1 to write {path}.");
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

    /// <summary>
    /// bUnit numbers event handlers in the order the renderer meets them, which a component boundary changes.
    /// </summary>
    [GeneratedRegex(@"(blazor:on[a-z]+)=""\d+""")]
    private static partial Regex EventHandlers();

    /// <summary>
    /// Blazor's scope attribute on every element a component with its own styles draws: a hash of the file's path,
    /// so it would change the golden when the file moves. Matches <c>&lt;div b-k3x9z2m1qa class="x"&gt;</c> and
    /// <c>&lt;div b-k3x9z2m1qa=""&gt;</c> (bUnit writes an empty value on a few elements) inside a tag, not the
    /// same letters in a text.
    /// </summary>
    [GeneratedRegex(@"(?<=<[^<>]*\s)b-[a-z0-9]{10}\b(="""")?")]
    private static partial Regex ScopeAttributes();

    /// <summary>A line break and its indentation next to a tag, inside text: indentation, not markup.</summary>
    [GeneratedRegex(@"\s*\n\s*(?=<)|(?<=>)\s*\n\s*")]
    private static partial Regex LineBreaksAtTags();
}
