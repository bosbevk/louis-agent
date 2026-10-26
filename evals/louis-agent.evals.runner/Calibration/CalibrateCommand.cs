using LouisAgent.Core.Messages;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Labels;
using LouisAgent.Evals.Runner.Arguments;
using LouisAgent.Evals.Runner.Messages;

namespace LouisAgent.Evals.Runner.Calibration;

/// <summary>
/// <c>--calibrate {eval} {grader}</c> (F18 step 6.3): runs a model grader on every labelled answer of the eval and prints how often it agrees with the person, then each answer where
/// it did not. It asks a model, so it is never run in CI: the guard is the first thing it does. Only a model grader is calibrated; a fixed rule needs no person to vouch for it.
/// </summary>
public static class CalibrateCommand
{
    public static async Task<int> RunAsync(CalibrateRequest request, IEval eval, Func<string, string?> getVariable, TextWriter stdout, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(getVariable("CI"))) throw new RunnerException(MessageText.Format(RunnerMessage.CalibrationNotInCi));

        var grader = ModelGraderOf(eval, request.Grader);
        // The labels folder may hold labels of more than one grader: this calibration reads those of the one it is asked about.
        var labels = eval.Labels.Where(label => label.Grader == grader.Name).ToList();
        if (labels.Count == 0) throw new RunnerException(MessageText.Format(RunnerMessage.NoLabels, new { Eval = eval.Name, Grader = grader.Name }));

        var report = await Labels.Calibration.RunAsync(grader, eval.Scenarios, labels, cancellationToken);
        foreach (string line in report.Lines()) stdout.WriteLine(line);
        return 0;
    }

    /// <summary>The eval's model grader of that name; a name it has not, or a fixed rule, is refused.</summary>
    public static IGrader ModelGraderOf(IEval eval, string name)
    {
        var grader = eval.Graders.FirstOrDefault(g => g.Name == name)
                     ?? throw new RunnerException(MessageText.Format(RunnerMessage.NoSuchGrader,
                         new { Eval = eval.Name, Grader = name, Graders = string.Join(", ", eval.Graders.Select(g => g.Name)) }));
        if (grader.Kind != GraderKind.Model) throw new RunnerException(MessageText.Format(RunnerMessage.GraderNotModel, new { Eval = eval.Name, Grader = name }));
        return grader;
    }
}
