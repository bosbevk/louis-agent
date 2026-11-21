using LouisAgent.Diagrams.Engine.Messages;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Engine.Tones;

/// <summary>
/// What a tone means, in words (CL60.2): the role a box of that colour plays in every picture. Each kind's key
/// says it, beside a sample in that tone.
/// </summary>
internal static class ToneMeanings
{
    /// <summary>The few words for a tone, such as "this project" for green.</summary>
    public static string Of(DiagramTone tone) => DiagramText.Format(tone switch
    {
        DiagramTone.Blue => DrawingMessage.LegendBlue,
        DiagramTone.Green => DrawingMessage.LegendGreen,
        DiagramTone.Violet => DrawingMessage.LegendViolet,
        DiagramTone.Teal => DrawingMessage.LegendTeal,
        DiagramTone.Red => DrawingMessage.LegendRed,
        _ => DrawingMessage.LegendAmber
    });
}
