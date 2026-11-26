namespace LouisAgent.Evals.Runs;

/// <summary>
/// Which play this is: of which run, which conversation, and which repeat of it, and the live run it replays when
/// it is a replay. Its recordings are kept under its trial's name.
/// </summary>
/// <param name="Run">The run's id.</param>
/// <param name="Conversation">The conversation's id in the eval file.</param>
/// <param name="Repeat">Which repeat, from 1.</param>
/// <param name="ReplayOf">The live run this play replays, or none for a live play.</param>
public sealed record RunTrial(string Run, string Conversation, int Repeat, string? ReplayOf = null)
{
    /// <summary>The play's own name within its run, as a session's task: <c>guide-what-is.1</c>.</summary>
    public string Task => $"{Conversation}.{Repeat}";

    /// <summary>The trial this play records under: <c>{run id}/{conversation}.{repeat}</c>.</summary>
    public string Name => $"{Run}/{Task}";

    /// <summary>The recorded trial a replay plays back, or none for a live play.</summary>
    public string? Replayed => ReplayOf is null ? null : $"{ReplayOf}/{Task}";

    /// <summary>The mark an ask writes where a case's id should be this trial's own.</summary>
    public const string TrialMark = "{trial}";

    /// <summary>
    /// This play's own id, the same for a live play and every replay of it: <c>{run id}-{repeat}</c> of the live run,
    /// letters, digits and dashes only, so an agent may name a file by it.
    /// </summary>
    public string Id => $"{ReplayOf ?? Run}-{Repeat}";

    /// <summary>
    /// An ask with this play's own id where it writes <see cref="TrialMark"/>, so an agent that keeps a case by its id
    /// starts every trial from nothing, and a replay sends the words that were recorded (FX257).
    /// </summary>
    public string Fill(string ask) => ask.Replace(TrialMark, Id, StringComparison.Ordinal);
}
