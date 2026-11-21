namespace LouisAgent.Api.Evals.Reviews.Store;

/// <summary>
/// Where people's grades of stored runs are kept: in memory for the tests, in MongoDB for Louis's runs.
/// </summary>
/// <remarks>
/// It sits beside the run store, not inside it. <c>HumanReviewStoreContract</c> holds every store to the same
/// behaviour.
/// </remarks>
public interface IHumanReviewStore
{
    /// <summary>Adds a grade. It never replaces an earlier one, so the history stays.</summary>
    Task AddAsync(HumanGrade grade, CancellationToken cancellationToken = default);

    /// <summary>Every grade of the run, earlier ones included, oldest first.</summary>
    Task<IReadOnlyList<HumanGrade>> OfRunAsync(string run, CancellationToken cancellationToken = default);

    /// <summary>Deletes the run's grades along with the run, and says how many there were.</summary>
    Task<int> DeleteRunAsync(string run, CancellationToken cancellationToken = default);
}
