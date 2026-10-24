using System.Diagnostics;
using System.Text.Json;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;

namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>One line of the runner's <c>--list</c>: an eval as the API reads it (the runner's own record, snake_case JSON).</summary>
public sealed record EvalLine(string Name, string? Identity, int Version, string Proves);

/// <summary>What reading the runner's listing gave: its evals, or why it couldn't be read (a text a person reads).</summary>
public sealed record EvalListingResult(IReadOnlyList<EvalLine> Evals, string? Failure);

/// <summary>
/// The runner's listing (F18 step 4.3): <c>dotnet {EVAL_RUNNER} --list</c>, as an argument list (ADR 0004), with a timeout. The API can't
/// reference the runner, so <see cref="EvalLine"/> is its own record. A listing that can't be read is a warning and no evals, never a failure
/// of the portal.
/// </summary>
public sealed class EvalListing(string command, IReadOnlyList<string> arguments, TimeSpan timeout, string? requiredFile = null)
{
    /// <summary>The runner's listing: <c>dotnet {runnerPath} --list</c>, and the runner's file must be there.</summary>
    public static EvalListing ForRunner(string runnerPath, TimeSpan? timeout = null) =>
        new("dotnet", [runnerPath, "--list"], timeout ?? TimeSpan.FromSeconds(30), runnerPath);

    // The runner writes snake_case (EvalJson.SnakeCase in the runner): name, identity, version, proves.
    private static readonly JsonSerializerOptions RunnerJson = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public EvalListingResult Read()
    {
        if (requiredFile is not null && !File.Exists(requiredFile)) return Failed(EvalListingMessage.Missing, new { Path = requiredFile });

        var start = new ProcessStartInfo(command) { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        try
        {
            using var process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEndAsync();
            if (!process.WaitForExit(timeout))
            {
                process.Kill(entireProcessTree: true);
                return Failed(EvalListingMessage.TimedOut, new { Seconds = timeout.TotalSeconds });
            }
            if (process.ExitCode != 0) return Failed(EvalListingMessage.ExitCode, new { Code = process.ExitCode });

            var evals = new List<EvalLine>();
            foreach (string line in output.Result.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                EvalLine? eval;
                try
                {
                    eval = JsonSerializer.Deserialize<EvalLine>(line, RunnerJson);
                }
                catch (JsonException)
                {
                    eval = null;
                }
                if (eval is null) return Failed(EvalListingMessage.NotJson, new { Line = line });
                evals.Add(eval);
            }
            return new EvalListingResult(evals, null);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return Failed(EvalListingMessage.Missing, new { Path = command });
        }
    }

    private static EvalListingResult Failed(EvalListingMessage message, object values) =>
        new([], MessageText.Format(message, values));
}
