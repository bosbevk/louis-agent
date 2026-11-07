namespace LouisAgent.Api.Evals.Calibrations.Store;

/// <summary>
/// Judge checks kept in memory: what the tests run against, and the reference the store contract is checked on. One
/// lock guards the list.
/// </summary>
public sealed class InMemoryCalibrationStore : ICalibrationStore
{
    private readonly Lock _lock = new();
    private readonly List<CalibrationRecord> _reports = [];

    /// <summary>Adds the report to the list.</summary>
    public Task AddAsync(CalibrationRecord report, CancellationToken cancellationToken = default)
    {
        lock (_lock) _reports.Add(report);
        return Task.CompletedTask;
    }

    /// <summary>The run's reports, newest first, copied out so nobody holds the lock.</summary>
    public Task<IReadOnlyList<CalibrationRecord>> OfRunAsync(string run, CancellationToken cancellationToken = default)
    {
        lock (_lock)
            return Task.FromResult<IReadOnlyList<CalibrationRecord>>(_reports.Where(r => r.Run == run)
                .OrderByDescending(r => r.At).ToArray());
    }

    /// <summary>Drops the run's reports and counts them.</summary>
    public Task<int> DeleteRunAsync(string run, CancellationToken cancellationToken = default)
    {
        lock (_lock) return Task.FromResult(_reports.RemoveAll(r => r.Run == run));
    }
}
