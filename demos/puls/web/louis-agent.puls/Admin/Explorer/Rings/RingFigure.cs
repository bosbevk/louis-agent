namespace LouisAgent.Puls.Admin.Explorer.Rings;

/// <summary>
/// What one mode's donut draws: its slices with their colours and hover titles, the figure in its middle, and the
/// word under that.
/// </summary>
public sealed record RingFigure(
    IReadOnlyList<double> Parts,
    IReadOnlyList<string> Colours,
    IReadOnlyList<string> Titles,
    string Centre,
    string Unit);
