using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Labels;

/// <summary>A labelled answer the judge did not agree with: the person's label, the judge's verdict (Unknown is "can't tell") and its reason.</summary>
public sealed record Disagreement(LabeledAnswer Answer, GradeOutcome Verdict, string Reason);

/// <summary>How often a model grader agreed with a person (F18 step 6.3): the count, and each answer where it did not, so the person can see why.</summary>
public sealed record CalibrationReport(int Agreed, int Total, IReadOnlyList<Disagreement> Disagreements)
{
    /// <summary>The report as the runner prints it: the agreement, then one line per disagreement.</summary>
    public IReadOnlyList<string> Lines() =>
    [
        MessageText.Format(CalibrationMessage.Agreement, new { Agreed, Total }),
        .. Disagreements.Select(d => MessageText.Format(CalibrationMessage.Disagreement, new
        {
            d.Answer.Scenario, d.Answer.Run, d.Answer.Repeat, d.Answer.Mode,
            Label = MessageText.Format(d.Answer.Meal ? CalibrationMessage.IsAMeal : CalibrationMessage.IsNotAMeal),
            Judge = MessageText.Format(d.Verdict switch
            {
                GradeOutcome.Pass => CalibrationMessage.IsAMeal,
                GradeOutcome.Fail => CalibrationMessage.IsNotAMeal,
                _ => CalibrationMessage.CantTell,
            }),
            d.Reason,
        })),
    ];
}

/// <summary>
/// Asks a model grader about every labelled answer (F18 step 6.3). The trial it builds has the answer in its turn, after the turns before it (empty: they are not what is labelled),
/// with the scenario's own questions and expectations, so the grader sees the ask as in a run. A pass agrees with a label of yes and a fail with a label of no; an Unknown agrees with
/// neither. It asks a model: never run in CI.
/// </summary>
public static class Calibration
{
    public static async Task<CalibrationReport> RunAsync(IGrader grader, IReadOnlyList<EvalScenario> scenarios, IReadOnlyList<LabeledAnswer> labels, CancellationToken cancellationToken)
    {
        var disagreements = new List<Disagreement>();
        foreach (var label in labels)
        {
            var scenario = scenarios.First(s => s.Id == label.Scenario);
            var grade = await grader.GradeAsync(TrialOf(label, scenario), cancellationToken);
            bool agrees = grade.Outcome == GradeOutcome.Pass && label.Meal || grade.Outcome == GradeOutcome.Fail && !label.Meal;
            if (!agrees) disagreements.Add(new Disagreement(label, grade.Outcome, grade.Reason));
        }
        return new CalibrationReport(labels.Count - disagreements.Count, labels.Count, disagreements);
    }

    private static Trial TrialOf(LabeledAnswer label, EvalScenario scenario)
    {
        var turns = Enumerable.Range(1, label.Turn).Select(n => new TurnTranscript(n, "calibration",
            n <= scenario.Input.Messages.Count ? scenario.Input.Messages[n - 1] : "", n == label.Turn ? [Answer(label.Answer)] : [], TimeSpan.Zero, null)).ToList();
        return new Trial("calibration", new EvalMode(label.Mode, "", []), label.Repeat, scenario, new Transcript(turns));
    }

    private static SessionEvent Answer(string text) =>
        SessionEvent.From("content_block_delta", JsonSerializer.SerializeToElement(new { type = "content_block_delta", index = 0, delta = new { type = "text_delta", text } }));
}
