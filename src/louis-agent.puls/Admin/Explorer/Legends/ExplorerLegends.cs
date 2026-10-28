using LouisAgent.Puls.Admin.Charts;
using LouisAgent.Puls.Admin.Explorer.Text;
using LouisAgent.Puls.Admin.Explorer.Tree;

namespace LouisAgent.Puls.Admin.Explorer.Legends;

/// <summary>The legends of the explorer's charts, each entry the colour its chart draws with.</summary>
public static class ExplorerLegends
{
    /// <summary>Each mode in its palette colour, named for who it is.</summary>
    public static IReadOnlyList<LegendEntry> Modes(RunTree tree) =>
        [.. tree.Columns.Select(c => new LegendEntry(ChartPalette.Series(c.I), ModeNames.Who(c.Mode)))];

    /// <summary>Puls's own part against the specialists', named for the roles.</summary>
    public static IReadOnlyList<LegendEntry> Parts(string roles) =>
        [new(ExplorerColours.Own, "Puls itself"), new(ExplorerColours.Spec, roles)];

    /// <summary>Routed right against wrong.</summary>
    public static readonly IReadOnlyList<LegendEntry> Routing =
        [new(ExplorerColours.Right, "routed right"), new(ExplorerColours.Wrong, "wrong")];

    /// <summary>Ingredients priced right, estimated, guessed, and with no price.</summary>
    public static readonly IReadOnlyList<LegendEntry> Accuracy =
    [
        new(ExplorerColours.Right, "right"), new(ExplorerColours.Estimate, "estimated"),
        new(ExplorerColours.Guess, "guessed"), new(ExplorerColours.Wrong, "no price")
    ];

    /// <summary>Input read from the prompt cache against input sent fresh.</summary>
    public static readonly IReadOnlyList<LegendEntry> Cache =
        [new(ChartPalette.Series(2), "from the cache"), new(ChartPalette.Series(0), "sent fresh")];

    /// <summary>Tool calls that worked against those refused or failed.</summary>
    public static readonly IReadOnlyList<LegendEntry> Tools =
        [new(ExplorerColours.Right, "worked"), new(ExplorerColours.Wrong, "refused or failed")];

    /// <summary>The sites read: South African shops, other South African sites, maps, and the rest.</summary>
    public static readonly IReadOnlyList<LegendEntry> Sources =
    [
        new(ExplorerColours.Right, "SA shops"), new(ChartPalette.Series(0), "other SA"),
        new(ChartPalette.Series(1), "maps"), new(ExplorerColours.Other, "elsewhere")
    ];

    /// <summary>The cost of conversations with a real answer against those without.</summary>
    public static readonly IReadOnlyList<LegendEntry> Value =
        [new(ExplorerColours.Right, "with an answer"), new(ExplorerColours.Wrong, "without")];
}
