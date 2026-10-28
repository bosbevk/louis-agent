using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;

namespace LouisAgent.Api.Evals.List;

/// <summary>
/// One eval as the runner's <c>--list</c> describes it: its name, version and what it proves, whose it is, where its
/// manual is, and what the portal shows before it ever ran (its dataset, categories and graders).
/// </summary>
/// <remarks>
/// Data Transfer Object (Fowler, PoEAA): the API can't reference the runner, so it reads the runner's line into its own
/// record. Everything after <c>Proves</c> is optional, so an older line that leaves a part out still reads.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record EvalLine(
    string Name,
    int Version,
    string Proves,
    string? Identity = null,
    string? Manual = null,
    EvalDatasetLine? Dataset = null,
    IReadOnlyList<string>? Categories = null,
    IReadOnlyList<EvalGraderLine>? Graders = null);

/// <summary>The dataset an eval's scenarios came from, by name and version.</summary>
public sealed record EvalDatasetLine(string Name, int Version);

/// <summary>One of an eval's graders: its name, its version and what it judges with.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public sealed record EvalGraderLine(string Name, int Version, EvalGraderKind Kind);

/// <summary>What reading the runner's listing gave: its evals, or why it couldn't be read, in words.</summary>
public sealed record EvalListingResult(IReadOnlyList<EvalLine> Evals, string? Failure);

/// <summary>
/// Asks the eval runner which evals it has (<c>dotnet {EVAL_RUNNER} --list</c>), with a timeout. If the listing can't
/// be read you get a warning and no evals, never a portal that won't load.
/// </summary>
/// <remarks>
/// The API can't reference the runner, so it reads the runner's lines into its own <see cref="EvalLine"/>. The runner
/// is started with an argument list, never through a shell.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
public sealed class EvalListing(
    string command,
    IReadOnlyList<string> arguments,
    TimeSpan timeout,
    string? requiredFile = null)
{
    internal const string DotNetHost = "dotnet";

    /// <summary>The runner's listing: <c>dotnet {runnerPath} --list</c>, and the runner's file must be there.</summary>
    public static EvalListing ForRunner(string runnerPath, TimeSpan? timeout = null) =>
        new(DotNetHost, [runnerPath, "--list"], timeout ?? TimeSpan.FromSeconds(30), runnerPath);

    // The runner writes snake_case (EvalJson.SnakeCase in the runner). A missing or null required field refuses the
    // line, not a default filled in (the review's fix: a {} line is no eval). A grader's kind travels by its wire
    // name, never a number, and a kind we don't know refuses the line too.
    private static readonly JsonSerializerOptions RunnerJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        Converters = { new JsonStringEnumConverter<EvalGraderKind>(allowIntegerValues: false) },
    };

    /// <summary>Runs the listing and reads what it wrote, or says why it couldn't.</summary>
    public EvalListingResult Read()
    {
        if (requiredFile is not null && !File.Exists(requiredFile))
            return Failed(EvalListingMessage.Missing, new { Path = requiredFile });

        var start = new ProcessStartInfo(command)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (string argument in arguments) start.ArgumentList.Add(argument);

        // A runner that can't start is the same warning as one that isn't there.
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
                // The runner's refusal is the last line it writes to stderr: that line is the reason, when there is
                // one.
                string? reason = errors.Result
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .LastOrDefault();
                return reason is null
                    ? Failed(EvalListingMessage.ExitCode, new { Code = process.ExitCode })
                    : Failed(EvalListingMessage.ExitCodeWithReason, new
                    {
                        Code = process.ExitCode,
                        Reason = reason
                    });
            }

            return Parse(output.Result);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return Failed(EvalListingMessage.Missing, new { Path = command });
        }
    }

    /// <summary>
    /// Reads the runner's listing text, one eval per line. One line that isn't an eval refuses the whole listing, and
    /// the reason quotes it.
    /// </summary>
    public static EvalListingResult Parse(string stdout)
    {
        var evals = new List<EvalLine>();
        foreach (string line in stdout.Split('\n',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
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

    /// <summary>No evals, and the reason in words a person reads.</summary>
    private static EvalListingResult Failed(EvalListingMessage message, object values) =>
        new([], MessageText.Format(message, values));
}
