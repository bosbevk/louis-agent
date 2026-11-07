using LouisAgent.Composition.Messages;
using System.Text.Json;
using System.Text.Json.Serialization;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Readers;
using LouisAgent.Evals.Readers.Listing;
using LouisAgent.Api.Evals.Messages;

namespace LouisAgent.Api.Evals.Studio;

/// <summary>
/// One check type as the runner's <c>--checks</c> gives it: its type, its kind and its settings' schema.
/// </summary>
/// <remarks>
/// The schema stays JSON: its shape is each check's settings record, decided in the runner (ADR 0041).
/// </remarks>
public sealed record CheckTypeLine(string Type, CheckKind Kind, JsonElement Parameters);

/// <summary>One reader as the runner's <c>--checks</c> gives it: its name and the kind it reads.</summary>
public sealed record ReaderLine(string Reader, ReaderKind Kind);

/// <summary>What reading the runner's catalogue gave: its checks and readers, or why it couldn't be read.</summary>
public sealed record CheckListingResult(
    IReadOnlyList<CheckTypeLine> Checks,
    IReadOnlyList<ReaderLine> Readers,
    string? Failure);

/// <summary>
/// Asks the eval runner for the catalogue the studio composes an eval from (<c>dotnet {EVAL_RUNNER} --checks</c>),
/// with a timeout, and reads its lines: a check has a <c>type</c>, a reader a <c>reader</c>.
/// </summary>
/// <remarks>
/// Built like <see cref="EvalListing"/>, through <see cref="RunnerProcess"/>: the API never carries the harness, so it
/// reads the runner's lines into its own records (finding R). A line that is neither, or names a kind the API doesn't
/// know, refuses the whole listing, quoting it, so a half-read catalogue never reaches a form.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
public sealed class CheckListing(
    string command,
    IReadOnlyList<string> arguments,
    TimeSpan timeout,
    string? requiredFile = null)
{
    // The runner writes snake_case; a missing or null field refuses the line, and a kind travels by its wire name,
    // never a number.
    private static readonly JsonSerializerOptions RunnerJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        Converters =
        {
            new JsonStringEnumConverter<CheckKind>(allowIntegerValues: false),
            new JsonStringEnumConverter<ReaderKind>(allowIntegerValues: false)
        },
    };

    /// <summary>
    /// The runner's catalogue: <c>dotnet {runnerPath} --checks</c>, and the runner's file must be there.
    /// </summary>
    public static CheckListing ForRunner(string runnerPath, TimeSpan? timeout = null) =>
        new(EvalListing.DotNetHost, [runnerPath, "--checks"], timeout ?? TimeSpan.FromSeconds(30), runnerPath);

    /// <summary>Runs the command and reads what it wrote, or says why it couldn't.</summary>
    public CheckListingResult Read()
    {
        var output = RunnerProcess.Run(command, arguments, timeout, requiredFile, "--checks");
        return output.Failure is { } failure ? new CheckListingResult([], [], failure) : Parse(output.Stdout);
    }

    /// <summary>Reads the lines, a check or a reader each; one that is neither refuses them all.</summary>
    public static CheckListingResult Parse(string stdout)
    {
        var checks = new List<CheckTypeLine>();
        var readers = new List<ReaderLine>();
        foreach (string line in stdout.Split('\n',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (Read(line))
            {
                case CheckTypeLine check:
                    checks.Add(check);
                    break;
                case ReaderLine reader:
                    readers.Add(reader);
                    break;
                default:
                    return new CheckListingResult([], [],
                        MessageText.Format(EvalListingMessage.NotACheck, new { Line = line }));
            }
        }

        return new CheckListingResult(checks, readers, null);
    }

    /// <summary>The line as a check or a reader, by the field it carries, or nothing when it is neither.</summary>
    /// <remarks>
    /// Guard clauses: not an object, then a check, then a reader; a line that won't read as its kind is none.
    /// </remarks>
    private static object? Read(string line)
    {
        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;
            if (root.TryGetProperty("type", out _)) return root.Deserialize<CheckTypeLine>(RunnerJson);
            if (root.TryGetProperty("reader", out _)) return root.Deserialize<ReaderLine>(RunnerJson);
            return null;
        }
        catch (JsonException)
        {
            // Not JSON, or a field missing or of a kind we don't know: not a line of the catalogue.
            return null;
        }
    }
}
