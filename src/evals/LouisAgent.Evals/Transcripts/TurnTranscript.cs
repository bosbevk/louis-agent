namespace LouisAgent.Evals.Transcripts;

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
    string? Error = null);
