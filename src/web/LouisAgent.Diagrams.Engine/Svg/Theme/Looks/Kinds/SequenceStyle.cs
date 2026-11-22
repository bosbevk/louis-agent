using LouisAgent.Choices;
using LouisAgent.Diagrams.Engine.Svg.Paints;

namespace LouisAgent.Diagrams.Engine.Svg.Theme.Looks.Kinds;

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
/// lifeline's are paints, each named here once with how it runs, and the drawing asks for it by that name.
/// </remarks>
internal static class SequenceStyle
{
    /// <summary>
    /// The ids of the sequence's own fades: a call's beam to the right, its mirror for a call to the left, and a
    /// lifeline's down the picture.
    /// </summary>
    public const string BeamPaint = "seq-beam", BackPaint = "seq-beam-back", LifePaint = "seq-life";

    /// <summary>How far down a lifeline its colour holds, before it fades to nothing at the picture's foot.</summary>
    private const double LifeHolds = 0.85;

    /// <summary>A call's beam to the right: across it, from its sender's colour to its receiver's.</summary>
    public static readonly Fade BeamFade = new(BeamPaint, PaintRun.Across, 0, 1);

    /// <summary>The beam's mirror, for a call to the left.</summary>
    public static readonly Fade BackFade = new(BackPaint, PaintRun.Across, 0, 1);

    /// <summary>A lifeline: down the picture, and out at its foot.</summary>
    public static readonly Fade LifeFade = new(LifePaint, PaintRun.Down, 0, LifeHolds, 1);

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
          .seq-cover { fill: var(--ground); }
          .seq-kind { fill: var(--ground); stroke: var(--violet-line); stroke-width: 1; stroke-opacity: .6; }
          .seq-tab { fill: var(--text); font: 600 12.5px var(--pic-sans); {{Halo}} }
          .seq-divide { fill: none; stroke: var(--mrq-hair); stroke-width: 1; stroke-dasharray: 4 6; }
          .seq-else { fill: var(--ground); stroke: var(--quiet); stroke-width: 1; stroke-opacity: .5; }
          .seq-else-word { fill: var(--quiet); font: 11px var(--mono); }
          .seq-ring.quiet { stroke: var(--quiet); }
          .seq-count { fill: var(--text); font: 700 10px var(--pic-sans); }
          .seq-quiet-node { fill: var(--ground); stroke: var(--quiet); stroke-width: 1.5; }
          .seq-swatch { fill: url(#{{MarqueeLook.GlassPaint}}); stroke: var(--mrq-sheet); stroke-opacity: .3; }
          """;

    /// <summary>
    /// What a sequence drawn as steps for a phone adds to the rules above (F169.7): a call's bar in the accent and an
    /// answer's dashed and quiet, a participant's square in its tone, its name, and a step's words, an answer's quiet.
    /// </summary>
    /// <remarks>
    /// Only the phone's drawing asks for these, so a sequence drawn wide, a picture in the docs among them, carries
    /// none of them. They live here, not in a style of their own: Louis's pick "C: inside the sequence's own style"
    /// (2026-11-22), Fowler's <i>Inline Class</i>, over a subfolder of the kinds' styles for a sequence's two and over
    /// a style beside its drawing. The step list is a sequence drawn another way, and the kinds' folder holds one
    /// style per kind of picture. The tones are on the squares only, as on the badges, so a bar has the accent and not
    /// its sender's colour. Every part is a plain colour, so a flat page has nothing of it to hide (F169.4).
    /// </remarks>
    /// <seealso href="https://refactoring.com/catalog/inlineClass.html">Fowler: Inline Class</seealso>
    public static string StepList(SvgTheme theme) =>
        $$"""

          .stl-bar { fill: var(--violet-line); }
          .stl-bar.quiet {
            fill: none; stroke: var(--quiet); stroke-opacity: .8; stroke-width: 2; stroke-dasharray: 3 4;
          }
          .stl-dot { fill: var(--quiet); }
          {{Toned(theme, "stl-dot")}}
          .stl-name { fill: var(--text); font: 600 12.5px var(--pic-sans); }
          .stl-words { fill: var(--text); font: 12.5px var(--pic-sans); }
          .stl-words.quiet { fill: var(--quiet); }
          """;

    /// <summary>A rule a tone: a badge is filled with its participant's tone.</summary>
    private static string Badges(SvgTheme theme) => Toned(theme, "seq-badge");

    /// <summary>A rule a tone for a part filled with its participant's tone: a badge, or a step's square.</summary>
    private static string Toned(SvgTheme theme, string part) => string.Join("\n", theme.Light.Tones.Keys
        .Select(tone => tone.Wire())
        .Select(name => $".{part}.tone-{name} {{ fill: var(--{name}-line); }}"));
}
