using System.Diagnostics;
using LouisAgent.Api.Evals.List;
using LouisAgent.Choices;

namespace LouisAgent.Api.Evals.Runs.Start;

/// <summary>
/// Where the runner finds things: the workspace it runs in, this API's address to call back, the eval runner's dll
/// and what runs it (<c>dotnet</c>).
/// </summary>
internal sealed record RunnerPaths(string WorkspaceRoot, string AgentApi, string EvalRunner, string EvalHost);

/// <summary>
/// Builds how one run's process starts. An eval run is <c>dotnet {EVAL_RUNNER} run {name} --repeat N</c>, with
/// <c>--web record</c> or <c>--replay {runId}</c> if asked; a re-score is <c>run {name} --rescore {runId}</c>; a judge
/// check is <c>--calibrate {name} {grader} --labels -</c>.
/// </summary>
/// <remarks>
/// The arguments go in as a list, never a command string, and the process sees only the allowlisted part of the API's
/// settings (<see cref="RunnerEnvironment"/>) plus what the run is told. Kept apart from <see cref="EvalProcess"/>,
/// which is about keeping one run at a time, not about how a process starts.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
internal static class RunnerStartInfo
{
    /// <summary>Everything the process for this run needs to start: program, arguments, folder and settings.</summary>
    public static ProcessStartInfo For(EvalEntry check, RunningEval run, RunnerPaths paths)
    {
        var start = new ProcessStartInfo(paths.EvalHost)
        {
            WorkingDirectory = paths.WorkspaceRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            // A re-score reads the stored run's export on its input, a judge check the people's grades; any other run
            // reads nothing.
            RedirectStandardInput = run.Input is not null,
            CreateNoWindow = true,
        };
        // Arguments as data (ADR 0004): the runner's path and its arguments are a list, never a command string.
        Arguments(check, run, paths, start.ArgumentList);

        // The process sees only what it reads and what it needs to start, never the API's secrets; the parent's copy
        // is replaced by the selection.
        var allowed = RunnerEnvironment.Select(Environment.GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(e => (string)e.Key, e => e.Value as string ?? ""));
        start.Environment.Clear();
        foreach (var (name, value) in allowed) start.Environment[name] = value;

        // Where the runner calls this API back; it reads the old name too, but the API names no product (ADR 0037).
        start.Environment["AGENT_API"] = paths.AgentApi;
        // The runner tags its sessions {BENCHMARK_RUN}:{mode}:{repeat}, so the ledger's rows find their run.
        start.Environment["BENCHMARK_RUN"] = run.Id;
        return start;
    }

    /// <summary>Writes the runner's arguments for a judge check, a re-score, or a fresh run.</summary>
    /// <remarks>
    /// Guard clauses (Fowler, <i>Replace Nested Conditional with Guard Clauses</i>): a judge check and a re-score each
    /// finish early, so what's left is a plain run, with no nesting.
    /// </remarks>
    private static void Arguments(EvalEntry check, RunningEval run, RunnerPaths paths,
        ICollection<string> arguments)
    {
        arguments.Add(paths.EvalRunner);
        // A judge check is no run: the runner checks the grader against the people's grades it reads from its input.
        if (run.Calibration is { } calibration)
        {
            foreach (string argument in new[] { "--calibrate", check.Id, calibration.Grader, "--labels", "-" })
                arguments.Add(argument);
            return;
        }

        arguments.Add("run");
        arguments.Add(check.Id);
        // A re-score takes the repeats, the web and the split from the stored run: its only flag is the run it grades
        // again.
        if (run.RescoreOf is { } rescoreOf)
        {
            arguments.Add("--rescore");
            arguments.Add(rescoreOf);
            return;
        }

        arguments.Add("--repeat");
        arguments.Add(run.Repeat.ToString());
        // The web: a replay of another run (the model's answers too), or a recording, or neither.
        if (run.Options.Replay is { } replayOf)
        {
            arguments.Add("--replay");
            arguments.Add(replayOf);
        }
        else if (run.Options.Web is { } web)
        {
            arguments.Add("--web");
            arguments.Add(web.Wire());
        }
    }
}
