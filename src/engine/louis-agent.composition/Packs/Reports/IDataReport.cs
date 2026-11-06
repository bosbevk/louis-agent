namespace LouisAgent.Composition.Packs.Reports;

/// <summary>
/// Numbers a pack keeps about itself, for whoever runs it to read: how often the recipe library was used, say. The
/// admin API lists each one at <c>/v1/admin/reports</c>, without knowing which pack it belongs to.
/// </summary>
/// <remarks>
/// Plugin (Fowler, PoEAA), the same seam as <see cref="Imports.IDataImport"/>: core names the shape and each pack plugs
/// its own in, so the API never references a pack (ADR 0027). Not an <see cref="IDataFeed"/>, which anyone may read:
/// what a sold endpoint's users did stays behind the admin key. A table, not a shape of its own, so a new report needs
/// no change to the API or the page. Reading is free of failure, as a tool's is (ADR 0005): a store that's down gives
/// an empty table.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0070-packs-publish-data-feeds.md">ADR 0070: packs publish data feeds</seealso>
/// <seealso href="https://martinfowler.com/eaaCatalog/plugin.html">Plugin (Fowler, PoEAA)</seealso>
public interface IDataReport
{
    /// <summary>"recipe-library": lowercase letters, digits and dashes, unique in the catalogue.</summary>
    string Name { get; }

    /// <summary>What the numbers are, one sentence, for the admin page.</summary>
    string Description { get; }

    /// <summary>The table as it stands now, or an empty one when its store can't be read.</summary>
    Task<ReportTable> ReadAsync(CancellationToken cancellationToken);
}
