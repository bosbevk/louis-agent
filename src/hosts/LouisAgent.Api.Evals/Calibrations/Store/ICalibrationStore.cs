namespace LouisAgent.Api.Evals.Calibrations.Store;

/// <summary>
/// Where the judge checks of stored runs are kept: in memory for the tests, in MongoDB for Louis's runs.
/// </summary>
/// <remarks>
/// It sits beside the run store and the people's grades, not inside them: a check is about the judge, not a run, so
/// the run list never shows one. <c>CalibrationStoreContract</c> holds every store to the same behaviour.
/// </remarks>
public interface ICalibrationStore
{
    /// <summary>Adds a report. It never replaces an earlier one, so the history stays.</summary>
    Task AddAsync(CalibrationRecord report, CancellationToken cancellationToken = default);

    /// <summary>Every report of the run, newest first.</summary>
    Task<IReadOnlyList<CalibrationRecord>> OfRunAsync(string run, CancellationToken cancellationToken = default);

    /// <summary>Deletes the run's reports along with the run, and says how many there were.</summary>
    Task<int> DeleteRunAsync(string run, CancellationToken cancellationToken = default);
}
