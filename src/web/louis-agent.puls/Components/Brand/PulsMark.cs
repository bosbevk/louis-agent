namespace LouisAgent.Puls.Components.Brand;

/// <summary>The ripple mark's geometry, shared by the logo (<c>PulsLogo</c>) and the app icon (<c>PulsTile</c>), so the two can't drift.</summary>
public static class PulsMark
{
    /// <summary>Radius and width of each ring cut out of the disc: the first clear from the green source's edge to the first orange band (no orange ring around the source), each after it thinner, with a thin even band of orange (3.5) between them.</summary>
    public static readonly (string Radius, string Width)[] Rings =
    [
        ("19.5", "28"), ("41.5", "9"), ("53.3", "7.6"),
        ("63.75", "6.3"), ("72.95", "5.1"), ("81", "4"), ("88", "3"),
    ];
}
