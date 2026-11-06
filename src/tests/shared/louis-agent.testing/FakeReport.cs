using LouisAgent.Composition.Packs.Reports;

namespace LouisAgent.Testing;

/// <summary>A report for the catalogue's and the API's tests: a name, and one row of counts it was given.</summary>
public sealed class FakeReport(string name, params long[] counts) : IDataReport
{
    /// <inheritdoc />
    public string Name => name;

    /// <inheritdoc />
    public string Description => $"The {name} report";

    /// <inheritdoc />
    public Task<ReportTable> ReadAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new ReportTable([.. counts.Select((_, at) => $"Column {at + 1}")],
            [new ReportRow("All days", counts)]));
}
