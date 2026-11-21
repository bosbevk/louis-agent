using System.Text.RegularExpressions;

namespace LouisAgent.Core.Tests;

/// <summary>
/// The diagrams are two projects (CL65.1, Louis: ".diagrams must be split into a diagrams --> and what builds the
/// diagrams"). <c>LouisAgent.Diagrams</c> says what a diagram is, so a pack's tool or an agent that only writes
/// or reads one takes it and no drawing code. That only holds while it never reaches for what builds one.
/// </summary>
public partial class DiagramsProjectTests
{
    private const string Diagrams = "src/web/LouisAgent.Diagrams", Engine = "src/web/LouisAgent.Diagrams.Engine";

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
            "ProjectReference ../../engine/LouisAgent.Choices/LouisAgent.Choices.csproj"
        }));

    /// <summary>What builds a diagram references what a diagram is, and no package.</summary>
    [Test]
    public void TheEngine_ReferencesTheDiagrams_AndNoPackage() =>
        Assert.That(References(Engine), Is.EqualTo(new[]
        {
            "ProjectReference ../LouisAgent.Diagrams/LouisAgent.Diagrams.csproj"
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
        var astray = ProjectSources.Of(Engine)
            .Where(source => !source.Text.Contains("namespace LouisAgent.Diagrams.Engine"))
            .Select(source => source.Path).ToList();

        Assert.That(astray, Is.Empty, string.Join('\n', astray));
    }

    /// <summary>
    /// A class a rule of a style is for. It finds <c>ground</c> in ".ground { fill" and <c>arrow</c> in
    /// ".arrow.tone-", and nothing in ".Select(" or in "opacity: .78".
    /// </summary>
    [GeneratedRegex(@"\.([a-z][a-z-]*[a-z])[ .,{]", RegexOptions.NonBacktracking)]
    private static partial Regex StyledClass();

    /// <summary>
    /// A text written in code, which is where a drawing names a shape's classes. It finds <c>pic-line reads</c>
    /// in <c>"pic-line reads"</c>, and nothing outside quotes.
    /// </summary>
    [GeneratedRegex("\"([^\"]*)\"", RegexOptions.NonBacktracking)]
    private static partial Regex Written();

    /// <summary>
    /// Every rule of the style every picture carries, and of the docs' frame's, is for a class some drawing writes
    /// (CL65.4). Every picture's file holds that style, so a rule for a look no kind is drawn in any more is in
    /// all of them. Each kind was redone in turn, and the rules of the look before stayed behind.
    /// </summary>
    [Test]
    public void EveryRuleOfTheStyleEveryPictureCarries_IsForAClassADrawingWrites()
    {
        var sources = ProjectSources.Of(Engine).ToList();
        string style = sources.Single(source => source.Path.EndsWith("/Svg/Theme/SvgStyle.cs")).Text;
        var written = sources.Where(source => !source.Path.Contains("/Svg/Theme/"))
            .SelectMany(source => Written().Matches(source.Text).SelectMany(text => text.Groups[1].Value.Split(' ')))
            .ToHashSet(StringComparer.Ordinal);

        var unused = StyledClass().Matches(style).Select(rule => rule.Groups[1].Value).Distinct()
            .Where(name => !written.Contains(name)).Order(StringComparer.Ordinal);

        Assert.That(unused, Is.Empty, "a rule for a class no drawing writes: take it out of the style");
    }

    /// <summary>
    /// A fade is named once, with the way it runs and where its stops are, beside the style that colours it
    /// (CL65.4). A drawing asks for a fade by its name and writes none of that itself. The look's glass and its
    /// accent were written out, with the same stops, in five drawings.
    /// </summary>
    [Test]
    public void NoDrawing_WritesHowAFadeRuns()
    {
        var writing = ProjectSources.Of(Engine)
            .Where(source => source.Path.Contains("/Svg/Renderers/"))
            .Where(source => source.Text.Contains("PaintRun."))
            .Select(source => source.Path[(Engine.Length + 1)..]).Order(StringComparer.Ordinal);

        Assert.That(writing, Is.Empty, "a drawing that says how a fade runs: name the fade beside its style");
    }
}
