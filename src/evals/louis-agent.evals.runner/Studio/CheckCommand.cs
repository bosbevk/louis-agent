using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Conformance;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.Evals.Runner.Messages;
using LouisAgent.Evals.Runner.Refusal;

namespace LouisAgent.Evals.Runner.Studio;

/// <summary>
/// <c>--check {folder}</c>: the studio's one judge of a composed eval (F49.5). It seals a manifest left without its
/// hash, loads the folder as strictly as a run would, with the runner's own catalogues, and holds it to the
/// conformance gate CI holds every eval to; then it writes one line per problem and, last, whether it was ok.
/// </summary>
/// <remarks>
/// Facade (Gang of Four) over the loader and the conformance check, for hosts that reach the harness only through
/// the runner's process (finding R). A refusal the loader throws is a problem like any other, in the words a run would
/// give, so the command ends well whenever it could look at the folder and the API reads every problem from stdout.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0058-an-eval-is-a-folder-of-files.md">ADR 0058: an eval is a folder of files</seealso>
public static class CheckCommand
{
    /// <summary>Checks the folder and writes its lines to <paramref name="output"/>.</summary>
    public static async Task<int> RunAsync(string folder, TextWriter output, CancellationToken cancellationToken)
    {
        var problems = await ProblemsAsync(folder, cancellationToken);
        foreach (string problem in problems)
            await output.WriteLineAsync(JsonSerializer.Serialize(new ProblemLine(problem), EvalJson.SnakeCase));
        await output.WriteLineAsync(JsonSerializer.Serialize(new OkLine(problems.Count == 0), EvalJson.SnakeCase));
        return 0;
    }

    /// <summary>What's wrong with the folder, in the order a run would meet it; none when it conforms.</summary>
    /// <remarks>Guard clause: a folder that isn't there; then a refusal from the loader is the one problem.</remarks>
    private static async Task<IReadOnlyList<string>> ProblemsAsync(string folder, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(folder))
            return [MessageText.Format(RunnerMessage.NoEvalFolder, new { Folder = folder })];
        try
        {
            ManifestSeal.Seal(folder);
            var eval = FileEval.Load(folder, RunnerChecks.Catalog, RunnerReaders.Catalog, "studio");
            var cases = ConformanceCases.Load(eval.Transcripts, eval.Scenarios);
            return await EvalConformance.CheckAsync(eval, cases, cancellationToken);
        }
        catch (Exception error) when (RefusalReport.IsRefusal(error) || error is JsonException)
        {
            return [error.Message];
        }
    }

    /// <summary>One problem the check found.</summary>
    private sealed record ProblemLine(string Problem);

    /// <summary>The last line: whether the folder conforms.</summary>
    private sealed record OkLine(bool Ok);
}
