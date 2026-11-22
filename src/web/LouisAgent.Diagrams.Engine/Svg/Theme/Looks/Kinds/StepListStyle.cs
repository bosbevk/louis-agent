using LouisAgent.Choices;

namespace LouisAgent.Diagrams.Engine.Svg.Theme.Looks.Kinds;

/// <summary>
/// What a sequence's list of steps adds to the sequence's look (F169.7): a call's bar in the look's accent and an
/// answer's dashed and quiet, a participant's square in its tone, its name, and a step's words, an answer's quiet.
/// </summary>
/// <remarks>
/// The list is drawn after <see cref="SequenceStyle"/>, so the beam, the dashes, the turn, the ring, the frames and
/// the note are the wide drawing's own rules, and only what the list adds is here. The tones are on the squares
/// only, as they are on the wide drawing's badges, so a tone says what a participant is and a bar has the accent,
/// not its sender's colour. Every part is a plain colour, so a flat page has nothing of it to hide (F169.4).
/// </remarks>
internal static class StepListStyle
{
    /// <summary>The style, for a theme's tones.</summary>
    public static string Of(SvgTheme theme) =>
        $$"""

          .stl-bar { fill: var(--violet-line); }
          .stl-bar.quiet {
            fill: none; stroke: var(--quiet); stroke-opacity: .8; stroke-width: 2; stroke-dasharray: 3 4;
          }
          .stl-dot { fill: var(--quiet); }
          {{Squares(theme)}}
          .stl-name { fill: var(--text); font: 600 12.5px var(--pic-sans); }
          .stl-words { fill: var(--text); font: 12.5px var(--pic-sans); }
          .stl-words.quiet { fill: var(--quiet); }
          """;

    /// <summary>A rule a tone: a square is filled with its participant's tone.</summary>
    private static string Squares(SvgTheme theme) => string.Join("\n", theme.Light.Tones.Keys
        .Select(tone => tone.Wire())
        .Select(name => $".stl-dot.tone-{name} {{ fill: var(--{name}-line); }}"));
}
