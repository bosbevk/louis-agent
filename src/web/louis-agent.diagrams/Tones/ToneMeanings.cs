using LouisAgent.Diagrams.Messages;

namespace LouisAgent.Diagrams.Tones;

/// <summary>
/// What a tone means, in words (CL60.2): the role a box of that colour plays in every picture. The docs' legend
/// says it, and so does a component diagram's own key.
/// </summary>
internal static class ToneMeanings
{
    /// <summary>The few words for a tone, such as "this project" for green.</summary>
    public static string Of(DiagramTone tone) => DiagramText.Format(tone switch
    {
        DiagramTone.Blue => DiagramMessage.LegendBlue,
        DiagramTone.Green => DiagramMessage.LegendGreen,
        DiagramTone.Violet => DiagramMessage.LegendViolet,
        DiagramTone.Teal => DiagramMessage.LegendTeal,
        DiagramTone.Red => DiagramMessage.LegendRed,
        _ => DiagramMessage.LegendAmber
    });
}
