namespace LouisAgent.Evals.Grading;

/// <summary>
/// A trial's outcome (F18 step 2a): a pass only when every required grader passes; Unknown when a required grader couldn't
/// tell, which is shown and never counted as a pass.
/// </summary>
public enum TrialOutcome
{
    Pass,
    Fail,
    Unknown,
}
