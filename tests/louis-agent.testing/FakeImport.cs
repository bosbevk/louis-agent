using LouisAgent.Core.Agent.Catalog.Imports;

namespace LouisAgent.Testing;

/// <summary>
/// An import for the catalogue's and the API's tests: a name and one file slot; it reports the files it was given, one
/// change each, and saves nothing anywhere.
/// </summary>
public sealed class FakeImport(string name) : IDataImport
{
    /// <inheritdoc />
    public string Name => name;

    /// <inheritdoc />
    public string Description => $"The {name} import";

    /// <inheritdoc />
    public IReadOnlyList<ImportSlot> Files { get; } = [new("file", "The one file")];

    /// <inheritdoc />
    public Task<ImportReport> PreviewAsync(IReadOnlyList<ImportFile> files, CancellationToken cancellationToken) =>
        Task.FromResult(Report(files, saved: false));

    /// <inheritdoc />
    public Task<ImportReport> SaveAsync(IReadOnlyList<ImportFile> files, CancellationToken cancellationToken) =>
        Task.FromResult(Report(files, saved: true));

    /// <summary>A report that names each file it was given.</summary>
    private static ImportReport Report(IReadOnlyList<ImportFile> files, bool saved) =>
        new(null, files.Count, saved, [.. files.Select(f => new ImportFact(f.Slot, f.FileName))], []);
}
