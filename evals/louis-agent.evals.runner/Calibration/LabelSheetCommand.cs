using LouisAgent.Core.Messages;
using LouisAgent.Evals.Labels;
using LouisAgent.Evals.Runner.Arguments;
using LouisAgent.Evals.Runner.Messages;

namespace LouisAgent.Evals.Runner.Calibration;

/// <summary>
/// <c>--label-sheet {eval} {grader} {runId} {folder} [{count}]</c> (F18 step 6.3): writes the files a person labels, from a stored run's protocol export on stdin (the lines
/// <c>--rescore</c> reads), for the scenarios that set what the grader reads. It asks no model and costs nothing.
/// </summary>
public static class LabelSheetCommand
{
    public static int Run(LabelSheetRequest request, IEval eval, IEnumerable<string> storedLines, TextWriter stdout)
    {
        var grader = CalibrateCommand.ModelGraderOf(eval, request.Grader);
        var names = LabelSheet.Write(eval.Scenarios, grader.Name, grader.ExpectedKeys, request.RunId, storedLines, request.Folder, request.Count);
        stdout.WriteLine(MessageText.Format(RunnerMessage.LabelSheetWritten, new { Count = names.Count, Folder = request.Folder }));
        return 0;
    }
}
