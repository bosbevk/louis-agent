namespace LouisAgent.Diagrams.Flow.Rows;

/// <summary>
/// The rule every picture's boxes were placed by before a flow got one of its own: each band's rows are as tall as
/// its own tallest box, and a column's boxes stand one after another, centred in their band.
/// </summary>
/// <remarks>
/// One of the two strategies behind <see cref="IRowRule"/>. It stays only while a kind of picture still draws with
/// it, and goes when the last one is replaced.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0103-a-diagram-is-data-on-a-row-grid.md">ADR 0103: a diagram is data on a row grid</seealso>
public sealed class CentredRows : IRowRule
{
    /// <summary>
    /// A band's row is as tall as its own tallest box, so a wide box in one group doesn't spread every other.
    /// </summary>
    public IReadOnlyList<double> RowHeights(IReadOnlyList<double> tallest, IReadOnlyList<IReadOnlySet<int>> spans) =>
        tallest;

    /// <summary>
    /// In each column a band's boxes stand one after another, centred in their band: a column with fewer boxes than
    /// the band has rows starts half the difference down.
    /// </summary>
    public IReadOnlyDictionary<string, double> RowTops(RowGrid grid)
    {
        var tops = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var column in grid.Columns)
            for (int band = 0; band < grid.Rows.Count; band++)
            {
                var members = column.Where(node => grid.BandOf(node) == band).ToList();
                double top = grid.Tops[band] + (grid.Tall(band, grid.Rows[band]) - grid.Tall(band, members.Count)) / 2;
                foreach (var node in members)
                {
                    tops[node.Id] = top;
                    top += grid.Heights[band] + grid.Gap;
                }
            }

        return tops;
    }
}
