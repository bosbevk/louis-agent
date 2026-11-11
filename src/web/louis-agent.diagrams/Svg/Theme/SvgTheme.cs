namespace LouisAgent.Diagrams.Svg.Theme;

/// <summary>The two looks one SVG carries, picked by the reader's screen.</summary>
public sealed record SvgTheme(SvgPalette Dark, SvgPalette Light)
{
    /// <summary>
    /// The site's own (Louis's pick P2): its dark Console palette, a box in the site's lighter tint (Louis: "a bit
    /// lighter gray"), and the same metallic greys turned over for a light screen (Louis's pick, CL60.1 step 3, since
    /// the site has no light theme of its own).
    /// </summary>
    public static SvgTheme Site { get; } = new(
        new SvgPalette("#070708", "#1b1d21", "#2c2f35", "#e6e8eb", "#ffffff", "#9aa0a8", "#15171a"),
        new SvgPalette("#ffffff", "#f6f7f8", "#d9dce0", "#15171a", "#070708", "#5f656d", "#eceef1"));
}
