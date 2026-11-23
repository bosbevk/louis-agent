namespace LouisAgent.Evals.Contracts.Transcripts;

/// <summary>
/// What happened in one turn: what was asked, what the agent answered as it sent it, each tool it called, how long the
/// turn took, and its error if it had one. A test writes one by hand; a run fills it from the session's stream.
/// </summary>
/// <param name="Ask">What the person asked.</param>
/// <param name="Answer">The agent's answer as it sent it, markdown and all.</param>
/// <param name="Calls">The tools the agent called, in order, each with its input and its result.</param>
/// <param name="Took">How long the turn took.</param>
/// <param name="Error">The turn's error, if it ended with one.</param>
public sealed record TurnTranscript(
    string Ask,
    string Answer,
    IReadOnlyList<ToolCallTranscript> Calls,
    TimeSpan Took,
    string? Error = null)
{
    /// <summary>
    /// Whether this turn says the same as another: the same ask, answer and error, and the same tool calls in the same
    /// order, each with the same tool, input, result and failure, compared as the text they are kept as. How long it
    /// took is left out, since a replay or a slower model takes another time to say the same thing.
    /// </summary>
    /// <remarks>
    /// Asked by name, never by the record's own equality, which compares the calls' lists by reference and would call
    /// two turns that read alike different (F126.4).
    /// </remarks>
    public bool SaysTheSameAs(TurnTranscript other) =>
        Ask == other.Ask && Answer == other.Answer && Error == other.Error && Calls.SequenceEqual(other.Calls);
}
