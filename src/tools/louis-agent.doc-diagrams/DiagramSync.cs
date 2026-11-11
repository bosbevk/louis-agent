using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg;

namespace LouisAgent.DocDiagrams;

/// <summary>
/// Finds the docs' diagram files and keeps each one's SVG what the file draws: a diagram's JSON sits in a
/// <c>diagrams</c> folder beside its doc, and its SVG beside the JSON (Louis's pick, CL60).
/// </summary>
public static class DiagramSync
{
    /// <summary>The name of the folder a doc's diagrams sit in.</summary>
    public const string Folder = "diagrams";

    /// <summary>Every diagram's JSON file in a <c>diagrams</c> folder under the roots, in order of path.</summary>
    public static IReadOnlyList<string> Find(IEnumerable<string> roots) =>
    [
        .. roots.Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateDirectories(root, Folder, SearchOption.AllDirectories))
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*.json"))
            .Order(StringComparer.Ordinal)
    ];

    /// <summary>
    /// Draws one diagram and writes its SVG when it differs from what is there, line endings aside, since a Windows
    /// checkout keeps its own; a check run only says it's stale.
    /// </summary>
    public static SyncOutcome Sync(string json, bool check)
    {
        string drawn = DiagramRenderers.Render(DiagramReader.Read(File.ReadAllText(json)));
        string svg = Path.ChangeExtension(json, ".svg");
        bool same = File.Exists(svg) && File.ReadAllText(svg).ReplaceLineEndings("\n") == drawn;
        if (same) return SyncOutcome.Unchanged;
        if (check) return SyncOutcome.Stale;
        File.WriteAllText(svg, drawn);
        return SyncOutcome.Written;
    }
}
