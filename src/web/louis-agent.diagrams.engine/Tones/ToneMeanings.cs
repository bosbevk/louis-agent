using LouisAgent.Diagrams.Engine.Messages;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Engine.Tones;

/// <summary>
/// What a tone means, in words (CL60.2): the role a box of that colour plays in every picture. The docs' legend
/// says it, and so does a component diagram's own key.
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
