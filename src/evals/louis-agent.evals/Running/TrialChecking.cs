using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Human;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Evals.Running;

/// <summary>
/// Grades one trial in a run (F18 step 4.5a): every grader of the eval, in the eval's order. A grader that throws gives
/// <see cref="GradeOutcome.Unknown"/> with its reason and one warning line, and the run goes on; a cancel of the run still stops it, a grader's own timeout (a judge's) does not. The
/// conformance check (<c>EvalConformance</c>) keeps its own loop on purpose: there a throw is a conformance failure with no grade,
/// because the check is about the grader; here it is an Unknown grade, because the run is about the trial.
/// </summary>
public static class TrialChecking
{
    public static async Task<IReadOnlyList<Grade>> GradeAsync(IEval eval, Trial trial, ILogger logger,
        CancellationToken cancellationToken)
    {
        var grades = new List<Grade>(eval.Checks.Count);
        foreach (ICheck grader in eval.Checks)
        {
            if (grader is HumanReviewCheck review && !review.AppliesTo(trial)) continue;
            try
            {
                grades.Add(await grader.GradeAsync(trial, cancellationToken));
            }
            catch (Exception error) when (!GraderFailures.IsRunCancel(error, cancellationToken))
            {
                logger.Log(LogLevel.Warning, EvalLogMessage.GraderThrew,
                    new
                    {
                        Eval = eval.Name,
                        Grader = grader.Name,
                        Scenario = trial.Scenario.Id,
                        Reason = error.Message
                    }, exception: error);
                grades.Add(Verdict.Unknown(grader.Name, grader.Version,
                    MessageText.Format(CommonGradeMessage.GraderThrew, new { Reason = error.Message })));
            }
        }

        return grades;
    }
}
