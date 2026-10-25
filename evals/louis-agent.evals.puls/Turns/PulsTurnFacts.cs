using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Puls.Turns;

/// <summary>
/// What the protocol records about a Puls turn (F18 step 5.3): who answered, the sub-session it continued, the calls Puls and its
/// specialists made, the hosts they read and the error, all from <see cref="TurnView"/>, so the facts and the scores read the turn alike.
/// </summary>
internal static class PulsTurnFacts
{
    public static TurnFacts Of(TurnTranscript turn)
    {
        var view = TurnView.From(turn);
        return new TurnFacts(view.Role, view.Continued, view.Calls.Count, view.SpecialistCalls.Count,
            ToolHosts.Of(view.Calls.Concat(view.SpecialistCalls)), view.Error);
    }
}
