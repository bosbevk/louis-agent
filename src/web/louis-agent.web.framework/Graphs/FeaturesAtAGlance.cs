using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Cards.Links;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Web.Framework.Content.Features;

namespace LouisAgent.Web.Framework.Graphs;

/// <summary>
/// Everything the framework does, at a glance (F160.1, step 11, Louis's pick A of three, "Areas at a glance"): the
/// framework highlighted, then a card an area in its own colour, with how many features it has and the first three
/// by name.
/// </summary>
/// <remarks>
/// Data for the diagram engine's cards kind, drawn by its own renderer on the page (ADR 0118), built from the
/// feature list itself, so a feature added or taken out changes the picture with the page. Chosen over every feature
/// boxed in one nested picture, too dense on a phone and the list again, and over the features along a turn's path,
/// which leaves the web's and the code's out.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0118-a-diagram-is-data-our-renderer-draws.md">ADR 0118: a diagram is data our renderer draws</seealso>
public static class FeaturesAtAGlance
{
    /// <summary>How many of an area's features its card names.</summary>
    private const int Named = 3;

    /// <summary>How many areas stand in the first row, under the framework; the rest stand in the second.</summary>
    private const int FirstRow = 3;

    /// <summary>The picture, as the engine reads it.</summary>
    public static readonly DiagramSource Source = Build();

    /// <summary>
    /// Builds the picture from the areas and their features: a verb, since it takes nothing to be made from (Louis's
    /// pick 2 of three, over <c>Of(features)</c> and building it in the field).
    /// </summary>
    private static DiagramSource Build()
    {
        // The page says what the framework is, so its card carries no line.
        var head = new Card("framework", DiagramIcon.Stack, "louis-agent", $"{FrameworkFeatures.All.Count} features", "",
            CardLook.Highlight);
        var areas = FrameworkFeatures.Areas.Select(Card).ToList();
        var links = areas.Take(FirstRow).Select(area => new CardLink(head.Id, area.Id)).ToList();
        return new DiagramSource(DiagramKind.Cards, "Everything it does, at a glance", Cards: new CardsSpec(
            [new CardRow([head]), new CardRow(areas.Take(FirstRow).ToList()), new CardRow(areas.Skip(FirstRow).ToList())],
            links));
    }

    /// <summary>An area's card: its name, its count, its first features by name, in its own colour.</summary>
    private static Card Card(FeatureArea area)
    {
        var features = FrameworkFeatures.All.Where(feature => feature.Area == area).ToList();
        string named = string.Join(", ", features.Take(Named).Select(feature => InASentence(feature.Name)));
        string more = features.Count > Named ? ", and more." : ".";
        return new Card(area.Id, area.Icon, area.Name, $"{features.Count} features", char.ToUpperInvariant(named[0]) +
            named[1..] + more, Tone: area.Tone);
    }

    /// <summary>
    /// A feature's name as it reads inside a sentence: "Only the tools it needs" and "A chat in one tag" start small,
    /// while a proper name of one word, Rider, keeps its capital, and "An HTTP API" reads "an HTTP API".
    /// </summary>
    private static string InASentence(string name)
    {
        string first = name.Split(' ')[0];
        bool word = name.Contains(' ') && first[1..].All(char.IsLower);
        return word ? char.ToLowerInvariant(name[0]) + name[1..] : name;
    }
}
