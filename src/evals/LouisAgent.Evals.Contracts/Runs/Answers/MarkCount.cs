namespace LouisAgent.Evals.Contracts.Runs.Answers;

/// <summary>How many marks were met, of how many, as a page says "5 of 7".</summary>
/// <param name="Met">The marks that were met.</param>
/// <param name="Of">All the marks counted.</param>
public sealed record MarkCount(int Met, int Of)
{
    /// <summary>No marks at all, where adding counts starts.</summary>
    public static MarkCount None { get; } = new(0, 0);

    /// <summary>Two counts added, as a run adds its repeats'.</summary>
    public static MarkCount operator +(MarkCount left, MarkCount right) =>
        new(left.Met + right.Met, left.Of + right.Of);
}
