namespace LouisAgent.Diagrams.Flow.Rows;

/// <summary>
/// Says which row each box of a flow stands on: how tall each band's rows are, and where the row a box stands on
/// starts. A band is a group's rows, or the rows of every box when the picture has no groups.
/// </summary>
/// <remarks>
/// Strategy (Refactoring.Guru), the second beside <see cref="Routing.IArrowRule"/> (Louis's pick, F134.8): the
/// layout stands a box on the row it is told, so a kind of picture that has not had its turn keeps its boxes centred
/// while a flow gets them level with what feeds them, and the centred rule is deleted without touching the layout
/// once every kind is replaced.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0103-a-diagram-is-data-on-a-row-grid.md">ADR 0103: a diagram is data on a row grid</seealso>
public interface IRowRule
{
    /// <summary>
    /// How tall each band's rows are, told each band's own tallest box and the columns each band has boxes in.
    /// </summary>
    IReadOnlyList<double> RowHeights(IReadOnlyList<double> tallest, IReadOnlyList<IReadOnlySet<int>> spans);

    /// <summary>Where the row each box stands on starts, a box an id.</summary>
    IReadOnlyDictionary<string, double> RowTops(RowGrid grid);
}
