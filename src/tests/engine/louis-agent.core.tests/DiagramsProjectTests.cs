using System.Text.RegularExpressions;

namespace LouisAgent.Core.Tests;

/// <summary>
/// The diagrams are two projects (CL65.1, Louis: ".diagrams must be split into a diagrams --> and what builds the
/// diagrams"). <c>louis-agent.diagrams</c> says what a diagram is, so a pack's tool or an agent that only writes
/// or reads one takes it and no drawing code. That only holds while it never reaches for what builds one.
/// </summary>
public partial class DiagramsProjectTests
{
    private const string Diagrams = "src/web/louis-agent.diagrams", Engine = "src/web/louis-agent.diagrams.engine";

    /// <summary>The separator a project file writes in a path, on every platform.</summary>
    private const char Backslash = (char)92;

    /// <summary>
    /// A type that builds a diagram, by its name. It finds <c>GraphLayout</c> in "class GraphLayout" and
    /// <c>SvgWriter</c> in "class SvgWriter", and nothing in "record GraphSpec".
    /// </summary>
    [GeneratedRegex(
        @"\b(?:class|record|interface)\s+(\w*(?:Layout|Renderer|Renderers|Drawing|Router|Style|Look)|SvgWriter)\b",
        RegexOptions.NonBacktracking)]
    private static partial Regex Builder();

    /// <summary>A project's references, each as its kind and what it names.</summary>
    private static List<string> References(string project)
    {
        string name = project[(project.LastIndexOf('/') + 1)..];
        string csproj = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, project, $"{name}.csproj"));
        return [.. Regex.Matches(csproj, "<(ProjectReference|PackageReference) Include=\"([^\"]+)\"")
            .Select(m => $"{m.Groups[1].Value} {m.Groups[2].Value.Replace(Backslash, '/')}")];
    }

    /// <summary>What a diagram is references the enum-name parser and nothing else: no project, no package.</summary>
    [Test]
    public void TheDiagramsProject_ReferencesOnlyTheEnumNameParser() =>
        Assert.That(References(Diagrams), Is.EqualTo(new[]
        {
            "ProjectReference ../../engine/louis-agent.choices/louis-agent.choices.csproj"
        }));

    /// <summary>What builds a diagram references what a diagram is, and no package.</summary>
    [Test]
    public void TheEngine_ReferencesTheDiagrams_AndNoPackage() =>
        Assert.That(References(Engine), Is.EqualTo(new[]
        {
            "ProjectReference ../louis-agent.diagrams/louis-agent.diagrams.csproj"
        }));

    /// <summary>
    /// What a diagram is holds no layout, no renderer, no drawing, no look and no writer, and names nothing of
    /// the engine's namespace.
    /// </summary>
    [Test]
    public void TheDiagramsProject_HoldsNothingThatBuildsADiagram()
    {
        var sources = ProjectSources.Of(Diagrams);
        var builders = sources.SelectMany(source => Builder().Matches(source.Text)
            .Select(match => $"{source.Path}: {match.Groups[1].Value}")).ToList();
        var reaching = sources.Where(source => source.Text.Contains("LouisAgent.Diagrams.Engine"))
            .Select(source => source.Path).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(sources, Is.Not.Empty);
            Assert.That(builders, Is.Empty, string.Join('\n', builders));
            Assert.That(reaching, Is.Empty, "a file of the diagrams names the engine's namespace");
        });
    }

    /// <summary>
    /// One builder puts every picture together (CL65.2), so no drawing writes a title, a light behind its top or
    /// a caption of its own. A cards picture keeps its own lights, one behind each highlighted card, and the
    /// ticket is a card in a chat with a look of its own.
    /// </summary>
    [Test]
    public void NoDrawing_WritesItsOwnTitleLightOrCaption()
    {
        var drawings = ProjectSources.Of(Engine).Where(source => source.Path.Contains("/Svg/Renderers/") &&
                                                       !source.Path.EndsWith("/PictureBuilder.cs")).ToList();
        string[] shared = ["\"mrq-title\"", "\"caption\"", ".Write(SvgTheme"];
        var own = drawings.Where(source => !source.Path.EndsWith("/TicketRenderer.cs"))
            .SelectMany(source => shared.Where(source.Text.Contains).Select(part => $"{source.Path}: {part}"))
            .Concat(drawings.Where(source => source.Text.Contains("\"mrq-light\"") &&
                                             !source.Path.EndsWith("/OverviewDrawing.cs"))
                .Select(source => $"{source.Path}: a light")).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(drawings, Has.Count.GreaterThan(10));
            Assert.That(own, Is.Empty, string.Join('\n', own));
        });
    }

    /// <summary>Every file of the engine is in the engine's namespace.</summary>
    [Test]
    public void EveryFileOfTheEngine_IsInItsNamespace()
    {
        var astray = ProjectSources.Of(Engine).Where(source => !source.Text.Contains("namespace LouisAgent.Diagrams.Engine"))
            .Select(source => source.Path).ToList();

        Assert.That(astray, Is.Empty, string.Join('\n', astray));
    }
}
