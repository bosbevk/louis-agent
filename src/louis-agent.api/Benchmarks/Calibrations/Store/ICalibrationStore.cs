namespace LouisAgent.Api.Benchmarks.Calibrations.Store;

/// <summary>
/// Where the judge checks of stored runs are kept (F18 step 6a.5b), beside the benchmark store and the people's grades, not in them: a check is of the judge, not a run, so the run
/// list never shows one. In memory for the tests, in MongoDB for Louis's runs; <c>CalibrationStoreContract</c> holds every store to this behaviour.
/// </summary>
public interface ICalibrationStore
{
    /// <summary>Adds a report. It never replaces an earlier one: the history is kept.</summary>
    Task AddAsync(CalibrationRecord report, CancellationToken cancellationToken = default);

    /// <summary>Every report of the run, newest first.</summary>
    Task<IReadOnlyList<CalibrationRecord>> OfRunAsync(string run, CancellationToken cancellationToken = default);

    /// <summary>Deletes the run's reports (with the run); how many there were.</summary>
    Task<int> DeleteRunAsync(string run, CancellationToken cancellationToken = default);
}
