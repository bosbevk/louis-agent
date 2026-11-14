using LouisAgent.Diagrams.Engine.Flow;
using LouisAgent.Diagrams.Engine.Svg.Theme;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Tests.Flow.Boxes;

/// <summary>
/// A box's icon and tone (CL60.2, Louis's pick "Both looks"): the icon white in a round badge of the box's tone, the
/// box tinted in it, its label from the left after the badge in the tone's dark shade. Both are optional, so a box
/// without them draws as before.
/// </summary>
public class FlowTonesTests
{
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
                              "type": "component",
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

    /// <summary>Every tone has its colours in both schemes, so nothing toned falls back to nothing.</summary>
    [Test]
    public void EveryTone_HasItsColours_DarkAndLight()
    {
        var tones = Enum.GetValues<DiagramTone>().Where(tone => tone != DiagramTone.Plain).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(tones.Where(tone => !SvgTheme.Site.Light.Tones.ContainsKey(tone)), Is.Empty, "light");
            Assert.That(tones.Where(tone => !SvgTheme.Site.Dark.Tones.ContainsKey(tone)), Is.Empty, "dark");
        });
    }
}
