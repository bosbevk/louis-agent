using System.Diagnostics;
using System.Text.Json;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;

namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>One line of the runner's <c>--list</c>: an eval as the API reads it (the runner's own record, snake_case JSON).</summary>
/// <remarks>Identity and the manual's path are optional: the runner writes them as null for an eval with none, and a line that leaves them out still reads (F18 step 7.1).</remarks>
public sealed record EvalLine(string Name, int Version, string Proves, string? Identity = null, string? Manual = null);

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

    // The runner writes snake_case (EvalJson.SnakeCase in the runner): name, identity, version, proves. A missing or null required
    // field refuses the line, not a default filled in (the review's fix: a {} line is no eval).
    private static readonly JsonSerializerOptions RunnerJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
    };

    public EvalListingResult Read()
    {
        if (requiredFile is not null && !File.Exists(requiredFile)) return Failed(EvalListingMessage.Missing, new { Path = requiredFile });

        var start = new ProcessStartInfo(command)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        try
        {
            using var process = Process.Start(start)!;
            // Both pipes are read while the process runs, so neither can fill and hang it.
            var output = process.StandardOutput.ReadToEndAsync();
            var errors = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(timeout))
            {
                process.Kill(entireProcessTree: true);
                return Failed(EvalListingMessage.TimedOut, new { Seconds = timeout.TotalSeconds });
            }
            if (process.ExitCode != 0)
            {
                // The runner's refusal is the last line it writes to stderr: that line is the reason, when there is one.
                string? reason = errors.Result.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault();
                return reason is null
                    ? Failed(EvalListingMessage.ExitCode, new { Code = process.ExitCode })
                    : Failed(EvalListingMessage.ExitCodeWithReason, new { Code = process.ExitCode, Reason = reason });
            }

            return Parse(output.Result);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return Failed(EvalListingMessage.Missing, new { Path = command });
        }
    }

    /// <summary>
    /// Reads the runner's listing text: one JSON eval per line. A line that isn't an eval (not JSON, or JSON missing its name, version
    /// or proof) refuses the whole listing, with the line in the reason.
    /// </summary>
    public static EvalListingResult Parse(string stdout)
    {
        var evals = new List<EvalLine>();
        foreach (string line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
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
            if (eval is null) return Failed(EvalListingMessage.NotAnEval, new { Line = line });
            evals.Add(eval);
        }
        return new EvalListingResult(evals, null);
    }

    private static EvalListingResult Failed(EvalListingMessage message, object values) =>
        new([], MessageText.Format(message, values));
}
