using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Engine.Cards;
using LouisAgent.Diagrams.Engine.Cards.Placed;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Svg.Paths;
using LouisAgent.Diagrams.Engine.Types;
using LouisAgent.Diagrams.Engine.Svg.Theme;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks.Kinds;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Overview;

/// <summary>
/// Draws a cards picture in the new look (F134.9, Louis's pick "Marquee"): its own small title, a light behind
/// each highlighted card, the rails between the rows, and each card by its look.
/// </summary>
/// <remarks>
/// It draws what <see cref="OverviewLayout"/> placed and decides no place itself. The look is the shared
/// <see cref="PictureLook"/>, then <see cref="MarqueeLook"/>, which a sequence wears too, then what
/// <see cref="CardsStyle"/> adds. The drawing names no colour: a part gets a class, and a fade gets a paint the
/// style fills with. <see cref="OverviewRails"/> draws a rail and <see cref="OverviewCards"/> a card.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0118-a-diagram-is-data-our-renderer-draws.md">ADR 0118: a diagram is data our renderer draws</seealso>
internal static class OverviewDrawing
{
    /// <summary>A cards picture's own number: a little less room above, since its first row is a headline.</summary>
    private static readonly PictureMeasures Measures = new(Head: 38);

    /// <summary>
    /// The light behind a highlighted card: how far under the card's top its middle is, and how far it reaches
    /// across and down. It reaches the picture's sides, where it has faded to nothing.
    /// </summary>
    private const double LightDrop = 88, LightAcross = 470, LightDown = 210;

    /// <summary>The cards picture a source holds, as one SVG file's text, drawn with a geometry.</summary>
    public static string Of(DiagramSource source, IGeometry geometry)
    {
        var placed = OverviewLayout.Place(source.Cards!, geometry: geometry);

        // A light behind each highlighted card, where another kind has one behind its top. The picture has no
        // key. Four fades are its own to ask for: the rail, the badge, a card's top edge, and the glass.
        return PictureBuilder.Titled(source.Title, Measures)
            .Type(TypeNames.Of(source))
            .Paint(CardsStyle.RailFade)
            .Paint(CardsStyle.BadgeFade)
            .Paint(MarqueeLook.EdgeFade)
            .Paint(MarqueeLook.GlassFade)
            .Lights(writer =>
            {
                foreach (var card in placed.Cards.Where(card => card.Card.Look == CardLook.Highlight))
                    Light(writer, card);
            })
            .Body("crd-cards", Cards(placed, geometry), placed.Width, placed.Height)
            .Caption(source.Caption)
            .Style(PictureLook.Of(SvgTheme.Site), MarqueeLook.Rules, CardsStyle.Rules)
            .Build();
    }

    /// <summary>
    /// The rails and the cards, from where the layout starts. A rail is drawn first, under its cards.
    /// </summary>
    private static SvgWriter Cards(PlacedOverview placed, IGeometry geometry)
    {
        var writer = new SvgWriter();
        foreach (var rail in placed.Rails) OverviewRails.Draw(writer, rail, placed.Width, geometry);
        foreach (var card in placed.Cards) OverviewCards.Draw(writer, card, geometry);
        return writer;
    }

    /// <summary>The light behind a highlighted card: an oval round the card's badge and title.</summary>
    private static void Light(SvgWriter writer, PlacedCard card)
    {
        double y = Measures.Head + card.Box.Top + LightDrop;
        string oval = new PathWriter().Oval(card.Box.Middle.X, y, LightAcross, LightDown).Text;
        writer.Path(oval, "mrq-light", false);
    }
}
