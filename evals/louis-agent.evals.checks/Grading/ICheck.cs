using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading;

/// <summary>
/// Scores one trial. A check names what it checks and whether a trial passes only when it passes (Required). Code
/// checks come first; a model check only where code can't judge.
/// </summary>
/// <remarks>
/// Strategy: each check is one way of scoring a trial, and an eval holds a list of them. It is the check contract,
/// kept outside the harness so a pack can ship a check (Separated Interface, Fowler).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public interface ICheck
{
    /// <summary>Its name, as the scores document names the key it fills ("priced-with-source").</summary>
    string Name { get; }

    /// <summary>Recorded with each grade, so a re-score can say which version gave it.</summary>
    int Version { get; }

    /// <summary>Fixed rule, model or human: what gives the verdict.</summary>
    CheckKind Kind { get; }

    /// <summary>
    /// The expected keys this check reads (<c>expected</c> in a scenario): a key no check reads is refused by the
    /// loader, so each criterion is checked by a named check. A check that reads none says so with an empty set.
    /// </summary>
    IReadOnlySet<string> ExpectedKeys { get; }

    /// <summary>A trial passes only when every required check passes.</summary>
    bool Required { get; }

    /// <summary>Scores the trial: a grade with its outcome and the reason a person reads.</summary>
    ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken);
}
