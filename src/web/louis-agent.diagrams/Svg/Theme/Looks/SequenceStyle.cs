using LouisAgent.Choices;

namespace LouisAgent.Diagrams.Svg.Theme.Looks;

/// <summary>
/// What a sequence's look adds to the shared ones (F134.10, Louis's pick of three designs, "Marquee"): a
/// participant's badge in its tone and its name, the lifeline, a call's beam either way, an answer's quiet line,
/// the words above them, a block's tab and its cases' divider, and the key's own parts.
/// </summary>
/// <remarks>
/// The tones are on the badges only, where a tone says what a participant is, as it does in a flow. Every call is
/// in the look's one accent, violet at its sender running to blue at its receiver, so a call no longer has its
/// sender's colour: the cost Louis was shown with the design. The small title, the light, the glass and the pill
/// are <see cref="MarqueeLook"/>'s, and the marching dots <see cref="PictureLook"/>'s. A beam's fade and a
/// lifeline's are paints the drawing defines under the ids here.
/// </remarks>
internal static class SequenceStyle
{
    /// <summary>
    /// The ids of the sequence's own fades: a call's beam to the right, its mirror for a call to the left, and a
    /// lifeline's down the picture.
    /// </summary>
    public const string BeamPaint = "seq-beam", BackPaint = "seq-beam-back", LifePaint = "seq-life";

    /// <summary>
    /// The halo that hides a lifeline behind words: a line of the ground's colour round each letter.
    /// </summary>
    private const string Halo =
        "paint-order: stroke; stroke: var(--ground); stroke-width: 5px; stroke-linejoin: round;";

    /// <summary>The style, for a theme's tones.</summary>
    public static string Of(SvgTheme theme) =>
        $$"""

          .seq-badge { fill: var(--quiet); }
          {{Badges(theme)}}
          .seq-badge-icon {
            fill: none; stroke: var(--ground); stroke-width: 2; stroke-linecap: round; stroke-linejoin: round;
          }
          .seq-name { fill: var(--text); font: 700 16px var(--pic-sans); letter-spacing: -.2px; }
          .seq-life { fill: url(#{{LifePaint}}); }
          .{{LifePaint}}-0 { stop-color: var(--violet-line); stop-opacity: .7; }
          .{{LifePaint}}-1 { stop-color: var(--blue-line); stop-opacity: .35; }
          .{{LifePaint}}-2 { stop-color: var(--blue-line); stop-opacity: 0; }
          .seq-beam { fill: url(#{{BeamPaint}}); }
          .seq-beam.back { fill: url(#{{BackPaint}}); }
          .seq-beam-glow { opacity: .45; filter: blur(4px); }
          .{{BeamPaint}}-0 { stop-color: var(--violet-line); }
          .{{BeamPaint}}-1 { stop-color: var(--blue-line); }
          .{{BackPaint}}-0 { stop-color: var(--blue-line); }
          .{{BackPaint}}-1 { stop-color: var(--violet-line); }
          .seq-tip { fill: none; }
          .seq-turn { fill: none; stroke: var(--violet-line); stroke-width: 2; stroke-linecap: round; }
          .seq-answer {
            fill: none; stroke: var(--quiet); stroke-opacity: .8; stroke-width: 1.5; stroke-dasharray: 3 6;
            stroke-linecap: round;
          }
          .seq-call-words { fill: var(--text); font: 600 12.5px var(--pic-sans); {{Halo}} }
          .seq-answer-words { fill: var(--quiet); font: 12.5px var(--pic-sans); {{Halo}} }
          .seq-note-words { fill: var(--quiet); font: 12px var(--pic-sans); }
          .seq-cover { fill: var(--ground); }
          .seq-kind { fill: var(--ground); stroke: var(--violet-line); stroke-width: 1; stroke-opacity: .6; }
          .seq-tab { fill: var(--text); font: 600 12.5px var(--pic-sans); {{Halo}} }
          .seq-divide { fill: none; stroke: var(--mrq-hair); stroke-width: 1; stroke-dasharray: 4 6; }
          .seq-else { fill: var(--ground); stroke: var(--quiet); stroke-width: 1; stroke-opacity: .5; }
          .seq-else-word { fill: var(--quiet); font: 11px var(--mono); }
          .seq-ring { fill: var(--ground); stroke: var(--violet-line); stroke-width: 1.5; }
          .seq-ring.quiet { stroke: var(--quiet); }
          .seq-count { fill: var(--text); font: 700 10px var(--pic-sans); }
          .seq-quiet-node { fill: var(--ground); stroke: var(--quiet); stroke-width: 1.5; }
          .seq-rule { fill: url(#{{MarqueeLook.EdgePaint}}); opacity: .7; }
          .seq-swatch { fill: url(#{{MarqueeLook.GlassPaint}}); stroke: var(--mrq-sheet); stroke-opacity: .3; }
          .seq-key { fill: var(--text); font: 12px var(--pic-sans); }
          """;

    /// <summary>A rule a tone: a badge is filled with its participant's tone.</summary>
    private static string Badges(SvgTheme theme) => string.Join("\n", theme.Light.Tones.Keys
        .Select(tone => tone.Wire())
        .Select(name => $".seq-badge.tone-{name} {{ fill: var(--{name}-line); }}"));
}
