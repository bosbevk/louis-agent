namespace LouisAgent.Api.Benchmarks.Calibrations.Store;

/// <summary>Judge checks in memory (F18 step 6a.5b): the contract's reference and what the tests run against. One lock over the list.</summary>
public sealed class InMemoryCalibrationStore : ICalibrationStore
{
    private readonly Lock _lock = new();
    private readonly List<CalibrationRecord> _reports = [];

    public Task AddAsync(CalibrationRecord report, CancellationToken cancellationToken = default)
    {
        lock (_lock) _reports.Add(report);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CalibrationRecord>> OfRunAsync(string run, CancellationToken cancellationToken = default)
    {
        lock (_lock) return Task.FromResult<IReadOnlyList<CalibrationRecord>>(_reports.Where(r => r.Run == run).OrderByDescending(r => r.At).ToArray());
    }

    public Task<int> DeleteRunAsync(string run, CancellationToken cancellationToken = default)
    {
        lock (_lock) return Task.FromResult(_reports.RemoveAll(r => r.Run == run));
    }
}
