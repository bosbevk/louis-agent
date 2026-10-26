namespace LouisAgent.Api.Benchmarks.Reviews;

/// <summary>People's grades in memory (F18 step 6a.2): the contract's reference and what the tests run against. One lock over the list.</summary>
public sealed class InMemoryHumanReviewStore : IHumanReviewStore
{
    private readonly Lock _lock = new();
    private readonly List<HumanGrade> _grades = [];

    public Task AddAsync(HumanGrade grade, CancellationToken cancellationToken = default)
    {
        lock (_lock) _grades.Add(grade);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<HumanGrade>> OfRunAsync(string run, CancellationToken cancellationToken = default)
    {
        lock (_lock) return Task.FromResult<IReadOnlyList<HumanGrade>>(_grades.Where(g => g.Run == run).OrderBy(g => g.At).ToArray());
    }

    public Task<int> DeleteRunAsync(string run, CancellationToken cancellationToken = default)
    {
        lock (_lock) return Task.FromResult(_grades.RemoveAll(g => g.Run == run));
    }
}
