using LouisAgent.Choices;

namespace LouisAgent.DocDiagrams.Tests.Projects;

/// <summary>
/// A project's design doc, as its first lines say it (CL65.5): which file it is and what kind of project it is
/// about. A doc says its kind in a quoted line, <c>&gt; **Kind:** library (`net10.0`) · **Path:** …</c>, and the
/// kind is the words before the first bracket, dot or comma.
/// </summary>
/// <remarks>
/// What is known about a doc is asked of this record, not worked out in each rule that reads one (ADR 0123).
/// </remarks>
/// <param name="Path">Where the doc is.</param>
/// <param name="Kind">The kind its line names, or none when it has no line or names no kind of the list.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public sealed record ProjectDoc(string Path, ProjectKind? Kind)
{
    /// <summary>How the line that states a doc's kind starts.</summary>
    private const string KindLine = "> **Kind:**";

    /// <summary>What ends the kind's words in that line: a bracket, the dot before the path, or a comma.</summary>
    private static readonly string[] KindEnds = [" (", " ·", ","];

    /// <summary>Every project doc, the framework's, the packs' and each demo's.</summary>
    public static IEnumerable<ProjectDoc> All() => Paths().Select(Read);

    /// <summary>
    /// Where every project doc is: a demo's root is the whole demo, so its <c>projects</c> folder sits deeper, in
    /// its <c>docs</c>.
    /// </summary>
    public static IEnumerable<string> Paths() => DiagramSync.Roots(TestPaths.RepoRoot)
        .Where(Directory.Exists)
        .SelectMany(root => Directory.EnumerateDirectories(root, "projects", SearchOption.AllDirectories))
        .SelectMany(folder => Directory.EnumerateFiles(folder, "*.md"))
        .Where(doc => System.IO.Path.GetFileName(doc) != "README.md");

    /// <summary>The doc at this path, read.</summary>
    public static ProjectDoc Read(string path) => new(path, KindOf(File.ReadLines(path)));

    /// <summary>The kind the doc's kind line names, read once with the one parser of a choice.</summary>
    private static ProjectKind? KindOf(IEnumerable<string> lines)
    {
        string? line = lines.FirstOrDefault(text => text.StartsWith(KindLine, StringComparison.Ordinal));
        if (line is null) return null;

        string rest = line[KindLine.Length..];
        int end = KindEnds.Select(stop => rest.IndexOf(stop, StringComparison.Ordinal))
            .Where(at => at >= 0).DefaultIfEmpty(rest.Length).Min();
        return SettingChoice.TryParse(rest[..end], out ProjectKind? kind) ? kind : null;
    }
}
