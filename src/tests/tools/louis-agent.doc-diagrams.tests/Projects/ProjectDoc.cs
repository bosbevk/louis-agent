using LouisAgent.Choices;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Sources.Types;
using LouisAgent.DocDiagrams.Tests.Docs;

namespace LouisAgent.DocDiagrams.Tests.Projects;

/// <summary>
/// A project's design doc, as a rule reads it (CL65.5): which file it is, the kind of project it is about, the
/// types of diagram it names of its own, and the pictures it shows with the type of each.
/// </summary>
/// <remarks>
/// <para>
/// A doc says its kind in a quoted line, <c>&gt; **Kind:** library (`net10.0`) · **Path:** …</c>: the kind is the
/// words before the first bracket, dot or comma. What it shows beyond its kind's floor is a line of its own,
/// <c>&gt; **Also shows:** `component`, `sequence`</c>, by the names a picture's <c>type</c> has.
/// </para>
/// <para>
/// What is known about a doc is asked of this record, not worked out in each rule that reads one (ADR 0123).
/// </para>
/// </remarks>
/// <param name="Path">Where the doc is.</param>
/// <param name="Kind">The kind its line names, or none when it has no line or names no kind of the list.</param>
/// <param name="Own">The types it names in its <c>**Also shows:**</c> line.</param>
/// <param name="Pictures">The pictures it links from its <c>diagrams</c> folder, as written.</param>
/// <param name="Shown">The type of each picture it shows, as the picture states it.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public sealed record ProjectDoc(
    string Path,
    ProjectKind? Kind,
    IReadOnlySet<DiagramType> Own,
    IReadOnlyList<string> Pictures,
    IReadOnlySet<DiagramType> Shown)
{
    /// <summary>How the line that states a doc's kind starts.</summary>
    private const string KindLine = "> **Kind:**";

    /// <summary>How the line that names what a doc shows beyond its floor starts.</summary>
    private const string AlsoLine = "> **Also shows:**";

    /// <summary>What ends the kind's words in its line: a bracket, the dot before the path, or a comma.</summary>
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
    public static ProjectDoc Read(string path)
    {
        string text = File.ReadAllText(path);
        string[] lines = text.ReplaceLineEndings("\n").Split('\n');
        string folder = System.IO.Path.GetDirectoryName(path)!;
        var pictures = DocLinks.Pictures(text)
            .Where(target => target.StartsWith($"{DiagramSync.Folder}/", StringComparison.Ordinal)).ToList();
        var shown = pictures.Select(picture => TypeOf(System.IO.Path.Combine(folder, picture)))
            .OfType<DiagramType>().ToHashSet();

        return new ProjectDoc(path, KindOf(lines), OwnOf(lines), pictures, shown);
    }

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

    /// <summary>The types the doc's <c>**Also shows:**</c> line names, each between backticks.</summary>
    private static HashSet<DiagramType> OwnOf(IEnumerable<string> lines)
    {
        string? line = lines.FirstOrDefault(text => text.StartsWith(AlsoLine, StringComparison.Ordinal));
        if (line is null) return [];

        // The parts between backticks are the names: every other part, from the second.
        var names = line[AlsoLine.Length..].Split('`').Where((_, part) => part % 2 == 1);
        return [.. names.Select(name => SettingChoice.TryParse(name, out DiagramType? type) ? type : null)
            .OfType<DiagramType>()];
    }

    /// <summary>The type a picture states, or its kind's own when it is one type; none for a chat's card.</summary>
    private static DiagramType? TypeOf(string svg)
    {
        string json = System.IO.Path.ChangeExtension(svg, ".json");
        if (!File.Exists(json)) return null;

        var source = DiagramReader.Read(File.ReadAllText(json));
        return source.Type ?? DiagramTypes.Own(source.Kind);
    }
}
