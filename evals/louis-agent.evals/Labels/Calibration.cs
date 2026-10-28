using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Labels;

/// <summary>A labelled answer the judge did not agree with: the person's label, the judge's verdict (Unknown is "can't tell") and its reason.</summary>
public sealed record Disagreement(LabeledAnswer Answer, GradeOutcome Verdict, string Reason);

/// <summary>What the judge said to one labelled answer.</summary>
public sealed record JudgedAnswer(LabeledAnswer Answer, GradeOutcome Verdict);

/// <summary>
/// How often a model grader agreed with a person (F18 step 6.3, per class in 6a.0): the count, each answer where it did not, and the agreement in each class of label. The headline can
/// be gamed: a judge that always passes agrees on every label that is a pass, so with 14 pass labels in 18 it scores 78% without judging anything. So the bar is held in each class:
/// <see cref="Bar"/> of the labels of the class (of the labelled-pass, and of the labelled-fail), and a class with no labels is untested, which does not pass.
/// </summary>
public sealed record CalibrationReport(
    int Agreed,
    int Total,
    IReadOnlyList<Disagreement> Disagreements,
    IReadOnlyList<JudgedAnswer> Judged)
{
    /// <summary>The share of a class's labels the judge must agree with: 90%.</summary>
    public const int Bar = 90;

    /// <summary>A class with fewer labels than this is said to be thin: a share of four is four answers.</summary>
    public const int FewLabels = 10;

    /// <summary>Whether the judge clears the bar in both classes.</summary>
    public bool Passes => ClassOf(pass: true).Clears && ClassOf(pass: false).Clears;

    /// <summary>The report as the runner prints it: the agreement, the agreement of each class, the result, then one line per disagreement.</summary>
    public IReadOnlyList<string> Lines() =>
    [
        MessageText.Format(CalibrationMessage.Agreement, new { Agreed, Total }),
        ClassLine(ClassOf(pass: true)),
        ClassLine(ClassOf(pass: false)),
        Result(),
        .. Disagreements.Select(d => MessageText.Format(CalibrationMessage.Disagreement, new
        {
            d.Answer.Scenario, d.Answer.Run, d.Answer.Repeat, d.Answer.Mode,
            Label = Word(d.Answer.Pass),
            Judge = MessageText.Format(d.Verdict switch
            {
                GradeOutcome.Pass => CalibrationMessage.Pass,
                GradeOutcome.Fail => CalibrationMessage.Fail,
                _ => CalibrationMessage.CantTell,
            }),
            d.Reason,
        })),
    ];

    private static string Word(bool pass) =>
        MessageText.Format(pass ? CalibrationMessage.Pass : CalibrationMessage.Fail);

    /// <summary>The judge's answers to the labels of one class: how many it passed, failed and could not tell, and how many agreed. Public since 6a.5a: the data line carries it.</summary>
    public ClassReport ClassOf(bool pass)
    {
        var verdicts = Judged.Where(j => j.Answer.Pass == pass).Select(j => j.Verdict).ToList();
        int agreed = verdicts.Count(v => v == (pass ? GradeOutcome.Pass : GradeOutcome.Fail));
        return new ClassReport(pass, verdicts.Count, agreed, verdicts.Count(v => v == GradeOutcome.Pass),
            verdicts.Count(v => v == GradeOutcome.Fail),
            verdicts.Count(v => v == GradeOutcome.Unknown));
    }

    private static string ClassLine(ClassReport c) => c.Total == 0
        ? MessageText.Format(CalibrationMessage.ClassNone, new { Label = Word(c.Pass) })
        : MessageText.Format(CalibrationMessage.ClassLine, new
        {
            Label = Word(c.Pass),
            c.Passed,
            c.Failed,
            c.CantTell,
            c.Agreed,
            c.Total,
            Percent = c.Percent,
            Few = c.Few ? MessageText.Format(CalibrationMessage.FewLabels) : "",
        });

    private string Result()
    {
        var shortClasses = new[] { ClassOf(pass: true), ClassOf(pass: false) }.Where(c => !c.Clears).ToList();
        if (shortClasses.Count == 0) return MessageText.Format(CalibrationMessage.ResultPass, new { Bar });
        return MessageText.Format(CalibrationMessage.ResultNotEnough, new
        {
            Short = string.Join("; ", shortClasses.Select(c => c.Total == 0
                ? MessageText.Format(CalibrationMessage.ShortNone, new { Label = Word(c.Pass) })
                : MessageText.Format(CalibrationMessage.ShortOf,
                    new { Label = Word(c.Pass), c.Agreed, c.Total, Bar }))),
        });
    }
}

/// <summary>One class of a person's labels (those they passed, or those they failed) and what the judge said to each: the total, how many agreed, and the judge's pass, fail and can't-tell counts.</summary>
public sealed record ClassReport(bool Pass, int Total, int Agreed, int Passed, int Failed, int CantTell)
{
    public int Percent => Total == 0 ? 0 : (int)Math.Round(100.0 * Agreed / Total, MidpointRounding.AwayFromZero);

    /// <summary>Whole numbers: at least 90% of a class, and a class with no labels clears nothing.</summary>
    public bool Clears => Total > 0 && Agreed * 100 >= Total * CalibrationReport.Bar;

    /// <summary>Fewer labels than <see cref="CalibrationReport.FewLabels"/>: a share of four is four answers.</summary>
    public bool Few => Total < CalibrationReport.FewLabels;
}

/// <summary>
/// Asks a model grader about every labelled answer (F18 step 6.3). The trial it builds has the answer in its turn, after the turns before it (empty: they are not what is labelled),
/// with the scenario's own questions and expectations, so the grader sees the ask as in a run. A pass agrees with a label of pass and a fail with a label of fail; an Unknown agrees with
/// neither. It asks a model: never run in CI.
/// </summary>
public static class Calibration
{
    public static async Task<CalibrationReport> RunAsync(IGrader grader, IReadOnlyList<EvalScenario> scenarios,
        IReadOnlyList<LabeledAnswer> labels, CancellationToken cancellationToken)
    {
        var judged = new List<JudgedAnswer>();
        var disagreements = new List<Disagreement>();
        foreach (var label in labels)
        {
            var scenario = scenarios.First(s => s.Id == label.Scenario);
            var grade = await grader.GradeAsync(TrialOf(label, scenario), cancellationToken);
            judged.Add(new JudgedAnswer(label, grade.Outcome));
            bool agrees = grade.Outcome == GradeOutcome.Pass && label.Pass ||
                          grade.Outcome == GradeOutcome.Fail && !label.Pass;
            if (!agrees) disagreements.Add(new Disagreement(label, grade.Outcome, grade.Reason));
        }

        return new CalibrationReport(labels.Count - disagreements.Count, labels.Count, disagreements, judged);
    }

    /// <summary>The trial as the judge is to see it: every turn of the labelled conversation, question and answer (6a.4f), the labelled answer last, so a follow-up is judged with what it relies on.</summary>
    private static Trial TrialOf(LabeledAnswer label, EvalScenario scenario)
    {
        var turns = label.Turns.Select((turn, i) =>
                new TurnTranscript(i + 1, "calibration", turn.Question, [Answer(turn.Answer)], TimeSpan.Zero, null))
            .ToList();
        return new Trial("calibration", new EvalMode(label.Mode, "", []), label.Repeat, scenario,
            new Transcript(turns));
    }

    private static SessionEvent Answer(string text) =>
        SessionEvent.From("content_block_delta",
            JsonSerializer.SerializeToElement(new
            { type = "content_block_delta", index = 0, delta = new { type = "text_delta", text } }));
}
