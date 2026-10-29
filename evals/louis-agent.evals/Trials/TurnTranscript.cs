using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Trials;

/// <summary>
/// One turn of a trial: the question, the session's events as they arrived, the turn's duration by the harness's clock, and
/// the failure that ended it, when one did (<see cref="TurnFailure"/>).
/// </summary>
public sealed record TurnTranscript(
    int Turn,
    string Session,
    string Question,
    IReadOnlyList<SessionEvent> Events,
    TimeSpan Duration,
    TurnFailure? Failure);
