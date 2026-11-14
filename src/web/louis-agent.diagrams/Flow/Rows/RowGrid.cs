using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Flow.Rows;

/// <summary>
/// The rows a flow's boxes can stand on, as a rule for the rows is told them: the columns with their boxes in
/// order, each box's band, and for each band where it starts, how tall its rows are and how many it has.
/// </summary>
/// <param name="Columns">The columns from the left, each with its boxes from the top.</param>
/// <param name="BandOf">Which band a box stands in.</param>
/// <param name="Tops">Where each band starts.</param>
/// <param name="Heights">How tall each band's rows are.</param>
/// <param name="Rows">How many rows each band has: as many as its fullest column has boxes.</param>
/// <param name="Gap">The room between two rows.</param>
/// <param name="Edges">The arrows, which say what feeds what.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record RowGrid(
    IReadOnlyList<IReadOnlyList<GraphNode>> Columns,
    Func<GraphNode, int> BandOf,
    IReadOnlyList<double> Tops,
    IReadOnlyList<double> Heights,
    IReadOnlyList<int> Rows,
    double Gap,
    IReadOnlyList<GraphEdge> Edges)
{
    /// <summary>How tall that many rows of a band are, with the gaps between them.</summary>
    public double Tall(int band, int rows) => rows * Heights[band] + Gap * (rows - 1);
}
