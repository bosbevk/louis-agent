namespace LouisAgent.Evals.Grading;

/// <summary>
/// What the judge's model answers, as a typed record: the reason first, then whether the criterion is met, so the
/// model says why before it says yes or no.
/// </summary>
/// <param name="Reason">Why the criterion is met or not, or what is missing to tell.</param>
/// <param name="Met">Whether the criterion is met.</param>
internal sealed record JudgeAnswer(string Reason, bool Met);
