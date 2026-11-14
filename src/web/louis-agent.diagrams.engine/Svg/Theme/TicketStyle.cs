using LouisAgent.Diagrams.Engine.Svg.Paints;

namespace LouisAgent.Diagrams.Engine.Svg.Theme;

/// <summary>
/// What a ticket adds to the look every picture wears (FX156): the light behind its head, its words' faces, the
/// chips its icons stand in and the well its note stands in. No ground behind it, so it sits on a chat as a card.
/// </summary>
/// <remarks>
/// <para>
/// The card is the look's own glass with the accent along its top, as a cards picture's and a sequence's are
/// (Louis: "it must follow the theme", "glass but not a ticket"). It had a look of its own, a filled band over a
/// torn edge, from before the pictures shared one.
/// </para>
/// <para>
/// Every colour is the page's variable first and the look's as the fallback (<c>var(--ticket-ink, …)</c>): a
/// ticket drawn inside a business's window takes the brand that window sets, and on the site it needs nothing.
/// The paper under the glass is solid, so the words read whatever page the card lies on.
/// </para>
/// </remarks>
internal static class TicketStyle
{
    /// <summary>The id of the fade that is the light behind a card's head.</summary>
    public const string LightPaint = "tkt-light";

    /// <summary>How far down the head the light turns from its first colour to its second.</summary>
    private const double LightTurns = 0.55;

    /// <summary>The light behind a card's head: down it, through its second colour, to nothing.</summary>
    public static readonly Fade LightFade = new(LightPaint, PaintRun.Down, 0, LightTurns, 1);

    /// <summary>
    /// The ticket's rules. They come after the look's and use its variables: the light behind the head is as
    /// strong as the look's own light behind a picture's top.
    /// </summary>
    public const string Rules =
        $$"""

          .ground { fill: none; }
          .ticket-paper { fill: var(--ticket-paper, var(--ground)); }
          .ticket { stroke: var(--ticket-line, var(--mrq-hair)); }
          .ticket-light { fill: url(#{{LightPaint}}); }
          .{{LightPaint}}-0 { stop-color: var(--ticket-accent, var(--violet-line)); stop-opacity: var(--mrq-glow); }
          .{{LightPaint}}-1 { stop-color: var(--ticket-accent, var(--blue-line)); stop-opacity: var(--mrq-glow-far); }
          .{{LightPaint}}-2 { stop-color: var(--ticket-accent, var(--blue-line)); stop-opacity: 0; }
          .mrq-edge-0, .mrq-edge-1, .mrq-edge-2 { stop-color: var(--ticket-accent, var(--violet-line)); }
          .ticket-rule { fill: var(--ticket-line, var(--mrq-hair)); }
          .ticket-kicker {
            fill: var(--ticket-accent, var(--violet-ink)); font: 600 10px var(--mono); letter-spacing: 1.4px;
          }
          .ticket-title {
            fill: var(--ticket-ink, var(--text)); font: 700 24px var(--ticket-title-font, var(--pic-sans));
            letter-spacing: -.3px;
          }
          .ticket-subtitle { fill: var(--ticket-quiet, var(--quiet)); font: 12px var(--pic-sans); }
          .ticket-icon-box, .ticket-well {
            fill: var(--ticket-chip, var(--pic-body)); stroke: var(--ticket-line, var(--pic-hair)); stroke-width: 1;
          }
          .ticket-icon {
            fill: none; stroke: var(--ticket-accent, var(--violet-line)); stroke-width: 2;
            stroke-linecap: round; stroke-linejoin: round;
          }
          .ticket-label { fill: var(--ticket-quiet, var(--quiet)); font: 11.5px var(--pic-sans); }
          .ticket-value { fill: var(--ticket-ink, var(--text)); font: 600 14px var(--pic-sans); }
          .ticket-note { fill: var(--ticket-quiet, var(--quiet)); font: 11.5px var(--pic-sans); }
          .ticket-pill { stroke-width: 1; }
          .ticket-pill-text { font: 600 11.5px var(--pic-sans); }
          """;
}
