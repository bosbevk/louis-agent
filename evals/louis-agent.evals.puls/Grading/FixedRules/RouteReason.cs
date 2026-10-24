using LouisAgent.Core.Messages;
using LouisAgent.Evals.Puls.Messages;
using LouisAgent.Evals.Puls.Transcripts;

namespace LouisAgent.Evals.Puls.Grading.FixedRules;

/// <summary>
/// Why a route was wrong, in words a person reads (shared by the cook's and the routing's graders). It describes the first turn,
/// the one the route is judged on: a later turn's error is no reason for the route's miss.
/// </summary>
internal static class RouteReason
{
    public static string Of(TurnView first, string? role) =>
        first.Error is not null ? MessageText.Format(GradeMessage.TurnFailed)
        : role is null ? MessageText.Format(GradeMessage.AnsweredByPuls)
        : MessageText.Format(GradeMessage.HandledBy, new { Role = role });
}
