using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Svg.Theme;

/// <summary>The two looks one SVG carries, picked by the reader's screen.</summary>
public sealed record SvgTheme(SvgPalette Dark, SvgPalette Light)
{
    /// <summary>
    /// The site's own (Louis's pick P2): its dark Console palette, a box in the site's lighter tint (Louis: "a bit
    /// lighter gray"), and the same metallic greys turned over for a light screen (Louis's pick, CL60.1 step 3, since
    /// the site has no light theme of its own). The accent is the site's blue, the eval screens' info colour, darker
    /// on a light screen so it still reads, each with a tint of itself for a highlighted card. The tones (CL60.2,
    /// Louis's pick "Both looks") are the target picture's: on a light screen a pale tint, a clear line and a dark
    /// ink of each hue; on a dark one a deep tint, a brighter line and a pale ink.
    /// </summary>
    public static SvgTheme Site { get; } = new(
        new SvgPalette("#070708", "#1b1d21", "#2c2f35", "#e6e8eb", "#ffffff", "#9aa0a8", "#15171a", "#7fa8e8",
            "#141b28", new Dictionary<DiagramTone, SvgTone>
            {
                [DiagramTone.Blue] = new("#15213a", "#6b9cf0", "#b9d0fa"),
                [DiagramTone.Green] = new("#13281b", "#4cc173", "#b6ebc6"),
                [DiagramTone.Violet] = new("#221a35", "#a985ec", "#d9c9f8"),
                [DiagramTone.Teal] = new("#0f2a27", "#3cc4b3", "#b3ece4"),
                [DiagramTone.Red] = new("#2e141c", "#ef6f8f", "#f8c2d0"),
                [DiagramTone.Amber] = new("#2c2010", "#f2ad52", "#f8dcb4")
            }),
        new SvgPalette("#ffffff", "#f6f7f8", "#d9dce0", "#15171a", "#070708", "#5f656d", "#eceef1", "#3b6fc4",
            "#eef3fc", new Dictionary<DiagramTone, SvgTone>
            {
                [DiagramTone.Blue] = new("#e3ecfa", "#3b78e7", "#1d4ea6"),
                [DiagramTone.Green] = new("#e2f3e7", "#2fa354", "#17703a"),
                [DiagramTone.Violet] = new("#ece4f7", "#8a55d6", "#5b2fa0"),
                [DiagramTone.Teal] = new("#def2ef", "#22a596", "#116f64"),
                [DiagramTone.Red] = new("#f9e1e6", "#e2456c", "#a51d42"),
                [DiagramTone.Amber] = new("#fbeedd", "#ef9a2d", "#a35d0c")
            }));
}
