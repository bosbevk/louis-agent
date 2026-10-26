namespace LouisAgent.Api.Benchmarks.Reviews;

/// <summary>
/// Where people's grades of stored runs are kept (F18 step 6a.2), beside the benchmark store and not in it: in memory for the tests, in MongoDB for Louis's runs.
/// <c>HumanReviewStoreContract</c> holds every store to this behaviour.
/// </summary>
public interface IHumanReviewStore
{
    /// <summary>Adds a grade. It never replaces an earlier one: the history is kept.</summary>
    Task AddAsync(HumanGrade grade, CancellationToken cancellationToken = default);

    /// <summary>Every grade of the run, earlier ones included, oldest first.</summary>
    Task<IReadOnlyList<HumanGrade>> OfRunAsync(string run, CancellationToken cancellationToken = default);

    /// <summary>Deletes the run's grades (with the run); how many there were.</summary>
    Task<int> DeleteRunAsync(string run, CancellationToken cancellationToken = default);
}
