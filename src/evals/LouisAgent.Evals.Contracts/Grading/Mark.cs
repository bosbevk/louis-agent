namespace LouisAgent.Evals.Contracts.Grading;

/// <summary>
/// One mark on a turn, the same for a fact and for a criterion: what was checked, whether it was met, and why, in one
/// plain line, so a red mark explains itself. A person may flip a criterion's mark, and then the other answer counts.
/// </summary>
/// <remarks>
/// Information Expert: the mark knows whether it was flipped, so it alone answers <see cref="CountsAsMet" /> and every
/// sum of a run asks it, never <see cref="Met" />. The flip sits on the mark itself (Louis's pick, 2026-11-23, "On the
/// mark itself"), over an audit list beside the run and over an event-sourced file of flips (Fowler).
/// </remarks>
/// <param name="Text">The fact's words or the criterion, as the eval file has it.</param>
/// <param name="Met">Whether it was met, as code or the judge said.</param>
/// <param name="Reason">Why: what code saw, or what the judge said.</param>
public sealed record Mark(string Text, bool Met, string Reason)
{
    /// <summary>A person's flip of the judge's answer, or none.</summary>
    /// <remarks>An init property, not a constructor parameter, so a run's file written before it still reads.</remarks>
    public Flip? Flip { get; init; }

    /// <summary>The answer that counts: the judge's, or the other one once a person flipped it.</summary>
    public bool CountsAsMet() => Flip is null ? Met : !Met;
}
