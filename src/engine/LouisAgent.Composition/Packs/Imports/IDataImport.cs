namespace LouisAgent.Composition.Packs.Imports;

/// <summary>
/// Data a person uploads into a pack, apart from its tools: Stats SA's price spreadsheets into the shop-prices pack,
/// say. The admin API lists each one and takes its files at <c>/v1/admin/imports/{name}</c>, without knowing which pack
/// it belongs to. A person sees the preview before anything is saved.
/// </summary>
/// <remarks>
/// Plugin (Fowler, PoEAA), the same seam as <see cref="IDataFeed"/> the other way round: core names the shape and each
/// pack plugs its own in, so the API never references a pack (ADR 0027). A file that can't be read is a problem in the
/// <see cref="ImportReport"/>, never a thrown exception, as a tool's failure is text (ADR 0005); the pack writes the
/// report's facts and problems from its own message enums (ADR 0009).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0070-packs-publish-data-feeds.md">ADR 0070: packs publish data feeds</seealso>
/// <seealso href="https://martinfowler.com/eaaCatalog/plugin.html">Plugin (Fowler, PoEAA)</seealso>
public interface IDataImport
{
    /// <summary>
    /// "statssa-average-prices": lowercase letters, digits and dashes, unique in the catalogue; the last part of its URL.
    /// </summary>
    string Name { get; }

    /// <summary>What the upload is and where a person gets it, one sentence, for the admin page.</summary>
    string Description { get; }

    /// <summary>The files it takes, one per slot, in the order the page shows them.</summary>
    IReadOnlyList<ImportSlot> Files { get; }

    /// <summary>Reads the files and says what saving them would store, without saving anything.</summary>
    Task<ImportReport> PreviewAsync(IReadOnlyList<ImportFile> files, CancellationToken cancellationToken);

    /// <summary>
    /// Reads the files and saves what the preview listed: what is new, and what is stored but has changed (a source's
    /// correction). What is stored and unchanged is left as it is.
    /// </summary>
    Task<ImportReport> SaveAsync(IReadOnlyList<ImportFile> files, CancellationToken cancellationToken);
}
