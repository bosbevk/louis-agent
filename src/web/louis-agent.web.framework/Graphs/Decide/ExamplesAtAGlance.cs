using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Cards.Links;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Web.Framework.Content.Examples;

namespace LouisAgent.Web.Framework.Graphs.Decide;

/// <summary>
/// The agents the framework already runs, at a glance (F160.1, step 12, Louis's pick A of three): the one engine
/// highlighted, then a card for each agent in its own colour, its identity in its pill and what it is in a line.
/// </summary>
/// <remarks>
/// Data for the diagram engine's cards kind, drawn by its own renderer on the page (ADR 0118), built from the
/// examples' own list. Chosen over the engine with each agent and its packs inside it, dense with the coding agent's
/// nine packs, and over the agents joined to the packs they share, whose lines cross.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0118-a-diagram-is-data-our-renderer-draws.md">ADR 0118: a diagram is data our renderer draws</seealso>
public static class ExamplesAtAGlance
{
    /// <summary>The picture, as the engine reads it.</summary>
    public static readonly DiagramSource Source = Build();

    /// <summary>Builds the picture from the examples: the engine, then a card an agent.</summary>
    private static DiagramSource Build()
    {
        // The page says what they share, so the engine's card carries no line.
        var engine = new Card("engine", DiagramIcon.Chip, "One engine", "louis-agent.core", "", CardLook.Highlight);
        var agents = FrameworkExamples.All.Select(example => new Card(Path.GetFileName(example.Identity), example.Icon,
            example.Name, Path.GetFileName(example.Identity), example.Line, Tone: example.Tone)).ToList();
        return new DiagramSource(DiagramKind.Cards, "Agents it already runs, at a glance", Cards: new CardsSpec(
            [new CardRow([engine]), new CardRow(agents)],
            [.. agents.Select(agent => new CardLink(engine.Id, agent.Id))]));
    }
}
