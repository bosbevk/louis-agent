namespace LouisAgent.Puls.Admin.Charts;

/// <summary>
/// The admin charts' series colours (F18 step 6a.3b: one place for the trend chart and the explorer's rings and bars, which each had a copy). The dataviz reference palette's dark column,
/// in its validated order: slots 1 to 4 for the token kinds, slots 1 to 3 for modes side by side. <see cref="Series"/> wraps round after the sixth.
/// </summary>
public static class ChartPalette
{
    public static readonly IReadOnlyList<string> Colours = ["#3987e5", "#d95926", "#199e70", "#c98500", "#d55181", "#008300"];

    public static string Series(int i) => Colours[i % Colours.Count];
}
