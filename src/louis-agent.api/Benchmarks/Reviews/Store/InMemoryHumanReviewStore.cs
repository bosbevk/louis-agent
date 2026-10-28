namespace LouisAgent.Api.Benchmarks.Reviews.Store;

/// <summary>
/// People's grades kept in memory: what the tests run against, and the reference the store contract is checked on.
/// One lock guards the list.
/// </summary>
public sealed class InMemoryHumanReviewStore : IHumanReviewStore
{
    private readonly Lock _lock = new();
    private readonly List<HumanGrade> _grades = [];

    /// <summary>Adds the grade to the list.</summary>
    public Task AddAsync(HumanGrade grade, CancellationToken cancellationToken = default)
    {
        lock (_lock) _grades.Add(grade);
        return Task.CompletedTask;
    }

    /// <summary>The run's grades, oldest first, copied out so nobody holds the lock.</summary>
    public Task<IReadOnlyList<HumanGrade>> OfRunAsync(string run, CancellationToken cancellationToken = default)
    {
        lock (_lock)
            return Task.FromResult<IReadOnlyList<HumanGrade>>(_grades.Where(g => g.Run == run).OrderBy(g => g.At)
                .ToArray());
    }

    /// <summary>Drops the run's grades and counts them.</summary>
    public Task<int> DeleteRunAsync(string run, CancellationToken cancellationToken = default)
    {
        lock (_lock) return Task.FromResult(_grades.RemoveAll(g => g.Run == run));
    }
}
