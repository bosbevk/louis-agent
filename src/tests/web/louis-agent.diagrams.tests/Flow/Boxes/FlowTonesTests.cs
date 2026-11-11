using System.Text.RegularExpressions;
using LouisAgent.Choices;
using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg;
using LouisAgent.Diagrams.Svg.Theme;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Tests.Flow.Boxes;

/// <summary>
/// A box's icon and tone (CL60.2, Louis's pick "Both looks"): the icon white in a round badge of the box's tone, the
/// box tinted in it, its label from the left after the badge in the tone's dark shade. Both are optional, so a box
/// without them draws as before.
/// </summary>
public class FlowTonesTests
{
    /// <summary>The browser, blue with its icon, calling a plain box.</summary>
    private static DiagramSource Calls() => new(DiagramKind.Flow, "Calls", Flow: new GraphSpec(
        [
            new GraphNode("browser", "Browser app (F61.2)", Icon: DiagramIcon.Browser, Tone: DiagramTone.Blue),
            new GraphNode("handlers", "AccountHandlers")
        ],
        [new GraphEdge("browser", "handlers")]));

    /// <summary>A box with an icon is wider by the badge's room, so its label still fits after the badge.</summary>
    [Test]
    public void AnIcon_WidensTheBoxByItsRoom()
    {
        const string label = "a label long enough to set the box's width by itself";
        var plain = GraphLayout.Place(new GraphSpec([new GraphNode("a", label)], [])).Nodes.Single();
        var badged = GraphLayout.Place(new GraphSpec([new GraphNode("a", label, Icon: DiagramIcon.Box)], []))
            .Nodes.Single();

        Assert.That(badged.W, Is.EqualTo(plain.W + FlowText.IconRoom));
    }

    /// <summary>A diagram's file names a box's icon and tone, and leaves them out when it has none.</summary>
    [Test]
    public void AFile_NamesTheIconAndTheTone()
    {
        const string json = """
                            {
                              "kind": "flow",
                              "title": "Calls",
                              "flow": {
                                "nodes": [
                                  { "id": "browser", "label": "Browser app", "icon": "browser", "tone": "blue" },
                                  { "id": "handlers", "label": "AccountHandlers" }
                                ],
                                "edges": []
                              }
                            }
                            """;

        var nodes = DiagramReader.Read(json).Flow!.Nodes;

        Assert.Multiple(() =>
        {
            Assert.That((nodes[0].Icon, nodes[0].Tone), Is.EqualTo((DiagramIcon.Browser, DiagramTone.Blue)));
            Assert.That((nodes[1].Icon, nodes[1].Tone), Is.EqualTo(((DiagramIcon?)null, DiagramTone.Plain)));
            Assert.That(DiagramWriter.Write(DiagramReader.Read(json)), Does.Not.Contain("plain"));
        });
    }

    /// <summary>
    /// A toned box is drawn in its tone with its icon in a badge and its label from the left; a plain one as before.
    /// </summary>
    [Test]
    public void AToneBox_WearsItsTone_AndItsBadge()
    {
        string svg = DiagramRenderers.Render(Calls());

        Assert.Multiple(() =>
        {
            Assert.That(Regex.Matches(svg, "class=\"box tone-blue\""), Has.Count.EqualTo(1));
            Assert.That(Regex.Matches(svg, "class=\"box\""), Has.Count.EqualTo(1));
            Assert.That(svg, Does.Contain("class=\"badge tone-blue\""));
            Assert.That(svg, Does.Contain("class=\"badge-icon\""));
            Assert.That(svg, Does.Match("class=\"label tone-blue\"[^>]*text-anchor=\"start\">Browser app"));
        });
    }

    /// <summary>Every tone has its colours in both looks, so no toned box falls back to nothing.</summary>
    [Test]
    public void EveryTone_HasItsColours_DarkAndLight()
    {
        string svg = DiagramRenderers.Render(Calls());

        Assert.Multiple(() =>
        {
            foreach (var tone in Enum.GetValues<DiagramTone>().Where(tone => tone != DiagramTone.Plain))
            {
                Assert.That(SvgTheme.Site.Light.Tones.ContainsKey(tone), Is.True, tone.ToString());
                Assert.That(SvgTheme.Site.Dark.Tones.ContainsKey(tone), Is.True, tone.ToString());
                Assert.That(svg, Does.Contain($".box.tone-{tone.Wire()}"), tone.ToString());
            }
        });
    }
}
