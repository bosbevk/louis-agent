using System.Diagnostics;

namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>Where the runner finds things: the workspace the process runs in, this API's address for the runner to call back, the eval runner's dll and what runs it (<c>dotnet</c>).</summary>
internal sealed record RunnerPaths(string WorkspaceRoot, string PulsApi, string EvalRunner, string EvalHost);

/// <summary>
/// The start of one run's process (F18 step 4.3; taken out of <see cref="BenchmarkRunner"/> in 6a.5b, which was at its size): an eval's
/// <c>dotnet {EVAL_RUNNER} run {name} --repeat N [--web record | --replay {runId}]</c>, a re-score's <c>run {name} --rescore {runId}</c>, or a judge check's
/// <c>--calibrate {name} {grader} --labels -</c> (6a.5b). An argument list (ADR 0004), and an environment that is the allowlisted part of the API's
/// (<see cref="RunnerEnvironment"/>, FX25) plus what the run is told. No interpreter and no script since F18 step 7.4.
/// </summary>
internal static class RunnerStartInfo
{
    public static ProcessStartInfo For(BenchmarkCheck check, BenchmarkRun run, RunnerPaths paths)
    {
        var start = new ProcessStartInfo(paths.EvalHost)
        {
            WorkingDirectory = paths.WorkspaceRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            // A re-score reads the stored run's export on stdin (F18 step 5b.2), a judge check the label lines (6a.5b); any other run reads nothing.
            RedirectStandardInput = run.Input is not null,
            CreateNoWindow = true,
        };
        // Arguments as data (ADR 0004): the runner's path and its arguments are a list, never a command string.
        Arguments(check, run, paths, start.ArgumentList);
        // FX25: the process sees only what it reads and what it needs to start, never the API's secrets; the parent's copy is replaced by the selection.
        var allowed = RunnerEnvironment.Select(Environment.GetEnvironmentVariables().Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(e => (string)e.Key, e => e.Value as string ?? ""));
        start.Environment.Clear();
        foreach (var (name, value) in allowed) start.Environment[name] = value;
        start.Environment["PULS_API"] = paths.PulsApi;
        // F31: the runner tags its sessions {BENCHMARK_RUN}:{mode}:{repeat}, so the ledger's rows find their run.
        start.Environment["BENCHMARK_RUN"] = run.Id;
        return start;
    }

    private static void Arguments(BenchmarkCheck check, BenchmarkRun run, RunnerPaths paths, ICollection<string> arguments)
    {
        arguments.Add(paths.EvalRunner);
        // A judge check (6a.5b) is no run: the runner calibrates the grader on the label lines it reads from stdin.
        if (run.Calibration is { } calibration)
        {
            foreach (string argument in new[] { "--calibrate", check.Id, calibration.Grader, "--labels", "-" }) arguments.Add(argument);
            return;
        }
        arguments.Add("run");
        arguments.Add(check.Id);
        // A re-score takes the repeats, the web and the split from the stored run: its only flag is the run it grades again.
        if (run.RescoreOf is { } rescoreOf)
        {
            arguments.Add("--rescore");
            arguments.Add(rescoreOf);
            return;
        }
        arguments.Add("--repeat");
        arguments.Add(run.Repeat.ToString());
        // The web (F18 step 5.5): a recording, or a replay of another run with the model's answers too (--replay).
        if (run.Options.Replay is { } replayOf)
        {
            arguments.Add("--replay");
            arguments.Add(replayOf);
        }
        else if (run.Options.Web is { } web)
        {
            arguments.Add("--web");
            arguments.Add(web.ToString().ToLowerInvariant());
        }
    }
}
