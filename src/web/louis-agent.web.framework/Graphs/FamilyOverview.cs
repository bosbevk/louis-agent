using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Cards.Links;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Web.Framework.Content;

namespace LouisAgent.Web.Framework.Graphs;

/// <summary>
/// A family of the framework at a glance (F164.1, Louis: "instead of listing one big overview do each under a
/// subheading and then use overview diagrams"): the family highlighted, with its folder, joined to its projects, three
/// to a row, each with a line of what it is, every card in the family's colour.
/// </summary>
/// <remarks>
/// Data for the diagram engine's cards kind, drawn by its own renderer on the page (ADR 0118), built from the family's
/// own record, so the picture names what the family ships and nothing else.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0118-a-diagram-is-data-our-renderer-draws.md">ADR 0118: a diagram is data our renderer draws</seealso>
public static class FamilyOverview
{
    /// <summary>How many projects stand in a row under the family.</summary>
    private const int PerRow = 3;

    /// <summary>The family's picture, as the engine reads it.</summary>
    public static DiagramSource For(FrameworkFamily family)
    {
        // The family's line is the section's own summary on the page, so the picture's top card carries none. Every
        // card wears the family's colour, the one the site talks of it in.
        var head = new Card(family.Id, family.Icon, family.Name, family.Folder, "", CardLook.Highlight, family.Tone);
        var projects = family.Projects
            .Select(project => new Card(project.Name, family.Icon, project.Name, null, project.What, Tone: family.Tone))
            .ToList();
        var rows = projects.Chunk(PerRow).Select(row => new CardRow(row)).Prepend(new CardRow([head])).ToList();

        // The family's line runs down to the first row; the rows under it follow on.
        var links = projects.Take(PerRow).Select(project => new CardLink(family.Id, project.Id)).ToList();
        return new DiagramSource(DiagramKind.Cards, $"{family.Name} at a glance", Cards: new CardsSpec(rows, links));
    }
}
