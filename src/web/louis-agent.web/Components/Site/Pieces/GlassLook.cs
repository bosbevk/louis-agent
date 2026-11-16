using System.Globalization;

namespace LouisAgent.Web.Components.Site.Pieces;

/// <summary>
/// The glass a panel is made of: its colour, how much of it from 0 (see-through) to 100, and what it does to what is
/// behind it. A panel writes it on itself, and the theme's names and the lens do the rest.
/// </summary>
/// <remarks>
/// Louis, 2026-11-16: "we should be able to give glass colors, transparent white etc.", "transparent meaning see
/// through glass and if it is transparent then the amount and then type water, etc". One value for the three, so a
/// panel's glass is said in one place and its markup asks it what to write (ADR 0123).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public sealed record GlassLook(GlassColour Colour, int Amount, GlassType Type)
{
    /// <summary>What every panel is unless a site says otherwise: see-through glass that ripples like water.</summary>
    public static GlassLook Default { get; } = new(GlassColour.Clear, 0, GlassType.Water);

    /// <summary>The colour as the panel writes it for its styles: none for clear glass.</summary>
    public string? ColourName => Colour switch
    {
        GlassColour.White => "white",
        GlassColour.Grey => "grey",
        _ => null
    };

    /// <summary>The type as the panel writes it for the lens.</summary>
    public string TypeName => Type switch
    {
        GlassType.Water => "water",
        GlassType.Frost => "frost",
        _ => "plain"
    };

    /// <summary>How much colour, as the panel's styles read it, kept from 0 to 100; none for clear glass.</summary>
    public string? Style => ColourName is null
        ? null
        : $"--web-glass-amount: {Math.Clamp(Amount, 0, 100).ToString(CultureInfo.InvariantCulture)}%";
}
