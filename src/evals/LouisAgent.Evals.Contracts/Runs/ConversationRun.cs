namespace LouisAgent.Evals.Contracts.Runs;

/// <summary>
/// One conversation of a run: each time it was played. It passed when it holds every repeat the run asked for and
/// each one passed, so a run that stopped part way never counts it as passed.
/// </summary>
/// <remarks>
/// Information Expert (Larman's GRASP): the record holds how many repeats the run asked for, so it can answer
/// <see cref="Passed"/> alone and a page asks it, chosen over the run answering for it and over keeping only whole
/// conversations (Louis, 2026-11-23). What a page asks of it is a method, never a property, so nothing new is
/// written into the run's file: the page's sums on the run's records, chosen over a presentation model per page
/// (Fowler) and over static helpers in the web project (Louis, 2026-11-23, by ADR 0123).
/// </remarks>
/// <param name="Id">The conversation's id in the eval file.</param>
/// <param name="Asked">How many repeats the run asked for.</param>
/// <param name="Repeats">The repeats played, in order.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public sealed record ConversationRun(string Id, int Asked, IReadOnlyList<RepeatRun> Repeats)
{
    /// <summary>Every repeat asked for was played, and each one passed.</summary>
    public bool Passed => Repeats.Count == Asked && Repeats.All(repeat => repeat.Passed);

    /// <summary>How many of its repeats passed.</summary>
    public int CountPassedRepeats() => Repeats.Count(repeat => repeat.Passed);

    /// <summary>Its first repeat that didn't pass; none when all of them passed.</summary>
    public RepeatRun? FindFirstFailedRepeat() => Repeats.FirstOrDefault(repeat => !repeat.Passed);

    /// <summary>What its first turn asked, from its first repeat; none when it holds no turn.</summary>
    public string? FindFirstAsk() => Repeats.FirstOrDefault()?.Turns.FirstOrDefault()?.Transcript.Ask;
}
