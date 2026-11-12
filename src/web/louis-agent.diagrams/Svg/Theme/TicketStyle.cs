using LouisAgent.Choices;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Svg.Theme;

/// <summary>
/// A ticket's look (F127.4.1), added after the theme's rules: a sans font, the band in the theme's banner colours,
/// the rows' and the note's parts, and a pill per tone. No ground behind it, so it sits on a chat as a card.
/// </summary>
/// <remarks>
/// Every colour is the page's variable first, the site's own as the fallback (<c>var(--ticket-band, …)</c>): a ticket
/// drawn inside a business's window takes the brand that window sets, and on the site it needs nothing (Louis's pick
/// Q3). The fallbacks are this picture's own variables, light and dark, as the rest of the library's are.
/// </remarks>
internal static class TicketStyle
{
    /// <summary>The ticket's rules, its band's colours in both looks.</summary>
    public static string Of(SvgTheme theme) =>
        $$"""
          svg {
            --sans: Geist, "Segoe UI", system-ui, -apple-system, Roboto, Arial, sans-serif;
            {{Band(theme.Light.Banner)}}
          }
          @media (prefers-color-scheme: dark) {
            svg { {{Band(theme.Dark.Banner)}} }
          }
          .ground { fill: none; }
          .ticket { fill: var(--ticket-paper, var(--panel)); stroke: var(--ticket-line, var(--line)); stroke-width: 1; }
          .ticket-band { fill: var(--ticket-band, var(--t-band)); }
          .ticket-kicker {
            fill: var(--ticket-band-ink, var(--t-band-ink)); fill-opacity: .78;
            font: 600 10.5px var(--sans); letter-spacing: 1.4px;
          }
          .ticket-title {
            fill: var(--ticket-band-ink, var(--t-band-ink)); font: 600 24px var(--ticket-title-font, var(--sans));
          }
          .ticket-subtitle {
            fill: var(--ticket-band-ink, var(--t-band-ink)); fill-opacity: .78; font: 12px var(--sans);
          }
          .ticket-tear { fill: none; stroke: var(--ticket-line, var(--line)); stroke-width: 2; stroke-dasharray: 4 4; }
          .ticket-icon-box { fill: var(--ticket-chip, var(--chip)); }
          .ticket-icon {
            fill: none; stroke: var(--ticket-accent, var(--accent)); stroke-width: 2;
            stroke-linecap: round; stroke-linejoin: round;
          }
          .ticket-label { fill: var(--ticket-quiet, var(--quiet)); font: 11.5px var(--sans); }
          .ticket-value { fill: var(--ticket-ink, var(--text)); font: 500 14px var(--sans); }
          .ticket-foot { fill: var(--ticket-chip, var(--chip)); }
          .ticket-note { fill: var(--ticket-quiet, var(--quiet)); font: 12px var(--sans); }
          .ticket-pill { fill: var(--ticket-band-ink, var(--t-band-ink)); }
          .ticket-pill-text { fill: var(--ticket-band, var(--t-band)); font: 600 11.5px var(--sans); }
          {{PillTones(theme)}}
          """;

    /// <summary>The band's colours as the picture's own variables.</summary>
    private static string Band(SvgTone banner) => $"--t-band: {banner.Fill}; --t-band-ink: {banner.Ink};";

    /// <summary>A toned pill wears its tone's strong colour, its words the band's.</summary>
    private static string PillTones(SvgTheme theme) => string.Join("\n", theme.Light.Tones.Keys
        .Select(tone => tone.Wire())
        .Select(name => $".ticket-pill.tone-{name} {{ fill: var(--{name}-line); }}"));
}
