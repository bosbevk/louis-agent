using System.Globalization;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Runner.Messages;
using LouisAgent.Choices;

namespace LouisAgent.Evals.Runner.Arguments;

/// <summary>What you asked the runner to do on the command line.</summary>
public abstract record CommandLineRequest;

/// <summary>You asked for <c>--list</c>: every eval the runner knows, one JSON object per line.</summary>
public sealed record ListRequest : CommandLineRequest;

/// <summary>
/// You asked for <c>--checks</c>: every check type the runner's evals can name, with its kind and the schema of its
/// settings, then every reader with its kind, one JSON object per line (F49.5's studio composes from it).
/// </summary>
public sealed record ChecksRequest : CommandLineRequest;

/// <summary>
/// <c>--check {folder}</c>: judge an eval folder the studio wrote, as strictly as a run would and against the
/// conformance gate, one JSON line per problem and then whether it was ok (F49.5).
/// </summary>
public sealed record CheckRequest(string Folder) : CommandLineRequest;

/// <summary>
/// <c>--import-promptfoo {file} {folder}</c>: a promptfoo file written out as an eval folder, what has no counterpart
/// named on stdout (F49.5).
/// </summary>
public sealed record ImportPromptfooRequest(string File, string Folder) : CommandLineRequest;

/// <summary><c>--export-promptfoo {folder}</c>: an eval folder as a promptfoo file, on stdout (F49.5).</summary>
public sealed record ExportPromptfooRequest(string Folder) : CommandLineRequest;

/// <summary>
/// Run one eval: <c>run {name} [--repeat N] [--web off|record|replay] [--of &lt;runId&gt;] [--replay &lt;runId&gt;]
/// [--split dev|held-out|all] | --rescore &lt;runId&gt;</c>. Each mode runs N times, and its web reads can be recorded
/// or replayed. With no web choice the instance's <c>WEB_REPLAY</c> decides; <paramref name="Of"/> is the recorded run
/// a replay reads. <c>--replay {runId}</c> replays that run's model answers too (<paramref name="Model"/>), so it costs
/// nothing. <c>--rescore {runId}</c> (<paramref name="RescoreOf"/>) grades a stored run's transcripts again, read from
/// stdin, without opening a session. A studio try (F49.5) adds <c>--folder {path}</c> (<paramref name="Folder"/>, the
/// eval's folder the studio wrote) and <c>--samples a,b</c> (<paramref name="Samples"/>, the scenarios to run).
/// </summary>
public sealed record RunRequest(
    string Name,
    int Repeat,
    WebReplayMode? Web = null,
    string? Of = null,
    bool Model = false,
    RunSplit Split = RunSplit.Dev,
    string? RescoreOf = null,
    string? Folder = null,
    string? Samples = null) : CommandLineRequest;

/// <summary>
/// <c>--calibrate {eval} {grader} [--labels -]</c>: asks a model grader about every answer a person labelled, and
/// prints how often the two agree. With <c>--labels -</c> (<paramref name="LabelsOnStdin"/>) the labels come from
/// stdin, one JSON object per line as the API writes them, instead of from the dataset.
/// </summary>
public sealed record CalibrateRequest(string Eval, string Grader, bool LabelsOnStdin = false) : CommandLineRequest;

/// <summary>
/// <c>--label-sheet {eval} {grader} {runId} {folder} [{count}]</c>: writes up to <paramref name="Count"/> label files
/// for a person to fill in, from a stored run's export on stdin (the same lines <c>--rescore</c> reads).
/// </summary>
public sealed record LabelSheetRequest(string Eval, string Grader, string RunId, string Folder, int Count)
    : CommandLineRequest;

/// <summary>
/// Reads the runner's command line into one request. Anything it can't make sense of is refused with a message that
/// says why.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public static class CommandLine
{
    /// <summary>
    /// The most repeats one run may ask for; more is cut down to this, as the API's <c>EvalProcess</c> does. The API
    /// can't reference the runner, so this is a copy and the two must match
    /// (src/hosts/louis-agent.api/Evals/Runs/EvalProcess.cs).
    /// </summary>
    public const int MaxRepeat = 5;

    /// <summary>How many labels a sheet asks for when you don't say: ten, the size of a calibration.</summary>
    public const int DefaultLabelCount = 10;

    /// <summary>Works out what the command line asks for, or refuses it.</summary>
    /// <remarks>Guard clauses (Fowler): one shape of the arguments per line, read from the top like a table.</remarks>
    public static CommandLineRequest Parse(IReadOnlyList<string> args)
    {
        if (args is ["--list"]) return new ListRequest();
        if (args is ["--checks"]) return new ChecksRequest();
        if (args is ["--check", var evalFolder]) return new CheckRequest(evalFolder);
        if (args is ["--import-promptfoo", var promptfoo, var target])
            return new ImportPromptfooRequest(promptfoo, target);
        if (args is ["--export-promptfoo", var exported]) return new ExportPromptfooRequest(exported);
        if (args is ["run", var name, ..]) return ParseRun(name, args.Skip(2).ToList());
        if (args is ["--calibrate", var calibrated, var grader]) return new CalibrateRequest(calibrated, grader);
        if (args is ["--calibrate", var fromStdin, var judged, "--labels", var where])
            return new CalibrateRequest(fromStdin, judged, LabelsOnStdin: ParseLabels(where));
        if (args is ["--label-sheet", var sheeted, var graded, var runId, var folder])
            return new LabelSheetRequest(sheeted, graded, ParseOf(runId), folder, DefaultLabelCount);
        if (args is ["--label-sheet", var named, var by, var run, var into, var count])
            return new LabelSheetRequest(named, by, ParseOf(run), into, ParseCount(count));
        if (args is ["--calibrate", ..] or ["--label-sheet", ..]) throw Refuse(RunnerMessage.Usage, null);
        if (args.Count == 0 || args[0] == "run") throw Refuse(RunnerMessage.Usage, null);
        throw Refuse(RunnerMessage.UnknownArgument, new { Argument = args[0] });
    }

    /// <summary>
    /// Reads the flags after a run's name, in any order; a flag given twice keeps its last value. Then it checks the
    /// flags fit together: a re-score takes none of a run's, a replay names its own run, and <c>--of</c> goes only with
    /// <c>--web replay</c>.
    /// </summary>
    /// <remarks>
    /// Replace Nested Conditional with Guard Clauses (Fowler): each refusal and each kind of run is one <c>if</c> from
    /// the top, so the order the checks run in is the order you read them.
    /// </remarks>
    private static RunRequest ParseRun(string name, IReadOnlyList<string> rest)
    {
        int repeat = 1;
        WebReplayMode? web = null;
        string? of = null, replay = null, rescore = null, folder = null, samples = null;
        var split = RunSplit.Dev;
        // Whether any flag that shapes a session or a run was given, whatever its value: a re-score takes none of them.
        bool shapesARun = false;

        // Every flag takes a value; one at the end without a value is refused as unknown.
        for (int i = 0; i < rest.Count; i++)
        {
            string flag = rest[i];
            if (i + 1 >= rest.Count) throw Refuse(RunnerMessage.UnknownArgument, new { Argument = flag });
            string value = rest[++i];
            switch (flag)
            {
                case "--repeat":
                    repeat = ParseRepeat(value);
                    shapesARun = true;
                    break;
                case "--web":
                    web = ParseWeb(value);
                    shapesARun = true;
                    break;
                case "--of": of = ParseOf(value); break;
                case "--replay":
                    replay = ParseOf(value);
                    shapesARun = true;
                    break;
                case "--split":
                    split = ParseSplit(value);
                    shapesARun = true;
                    break;
                case "--rescore": rescore = ParseOf(value); break;
                case "--folder": folder = value; break;
                case "--samples": samples = ParseSamples(value); break;
                default: throw Refuse(RunnerMessage.UnknownArgument, new { Argument = flag });
            }
        }

        // A re-score grades what is stored, so nothing that shapes a run may come with it, nor a studio folder.
        if (rescore is not null && (shapesARun || of is not null))
            throw Refuse(RunnerMessage.RescoreExcludesRunFlags, null);
        if (rescore is not null && (folder is not null || samples is not null))
            throw Refuse(RunnerMessage.RescoreExcludesFolder, null);
        if (rescore is not null) return new RunRequest(name, repeat, RescoreOf: rescore);

        // --replay already names the run and replays its web, so --web and --of would only contradict it.
        if (replay is not null && (web is not null || of is not null))
            throw Refuse(RunnerMessage.ReplayExcludesWebAndOf, null);
        if (replay is not null)
            return new RunRequest(name, repeat, WebReplayMode.Replay, replay, Model: true, Split: split,
                Folder: folder, Samples: samples);

        if (web == WebReplayMode.Replay && of is null) throw Refuse(RunnerMessage.ReplayNeedsOf, null);
        if (web != WebReplayMode.Replay && of is not null) throw Refuse(RunnerMessage.OfNeedsReplay, null);
        return new RunRequest(name, repeat, web, of, Split: split, Folder: folder, Samples: samples);
    }

    /// <summary>
    /// The scenarios a try runs: kebab-case ids, commas between, letters and digits joined by single dashes, as the
    /// scenario files are named.
    /// </summary>
    private static string ParseSamples(string value)
    {
        var ids = value.Split(',', StringSplitOptions.TrimEntries);
        bool kebab = ids.All(id => id.Length > 0 && id.Split('-').All(part =>
            part.Length > 0 && part.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9')));
        return kebab ? string.Join(",", ids) : throw Refuse(RunnerMessage.BadSamples, new { Value = value });
    }

    /// <summary>
    /// <c>--labels</c> only takes <c>-</c>, meaning stdin: otherwise the labels already live in the dataset's folder.
    /// </summary>
    private static bool ParseLabels(string value) =>
        value == "-" ? true : throw Refuse(RunnerMessage.LabelsOnlyStdin, new { Value = value });

    /// <summary>How many labels a sheet should ask for: a whole number, one or more.</summary>
    private static int ParseCount(string value)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int n) || n < 1)
            throw Refuse(RunnerMessage.BadLabelCount, new { Value = value });
        return n;
    }

    /// <summary>How many times to repeat each mode, kept between one and the most a run may ask for.</summary>
    private static int ParseRepeat(string value)
    {
        if (!int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int n))
            throw Refuse(RunnerMessage.BadRepeat, new { Value = value });
        return Math.Clamp(n, 1, MaxRepeat);
    }

    /// <summary>
    /// Whether the web is off, recorded or replayed, read by its name whatever the case, never as a number.
    /// </summary>
    private static WebReplayMode ParseWeb(string value)
    {
        if (!SettingChoice.TryParse(value, out WebReplayMode? web) || web is null)
            throw Refuse(RunnerMessage.UnknownWeb,
                new
                {
                    Value = value,
                    Supported = string.Join(", ", SettingChoice.Names<WebReplayMode>())
                });
        return web.Value;
    }

    /// <summary>
    /// Which scenarios to run: <c>dev</c>, <c>held-out</c> (or the old <c>held_out</c>) or <c>all</c>, never a number.
    /// </summary>
    private static RunSplit ParseSplit(string value)
    {
        if (!SettingChoice.TryParse(value, out RunSplit? split, RunSplits.Aliases) || split is null)
            throw Refuse(RunnerMessage.UnknownSplit,
                new
                {
                    Value = value,
                    Supported = string.Join(", ", SettingChoice.Names<RunSplit>())
                });
        return split.Value;
    }

    /// <summary>
    /// A run id holds only letters, digits, <c>_</c> and <c>-</c>: the API builds a trial's key from it, so it must
    /// never carry a separator.
    /// </summary>
    private static string ParseOf(string value)
    {
        if (value.Length == 0 || !value.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-'))
            throw Refuse(RunnerMessage.BadOf, new { Value = value });
        return value;
    }

    /// <summary>Builds the refusal you see, from its message and the values it names.</summary>
    private static RunnerException Refuse(RunnerMessage message, object? values) =>
        new(MessageText.Format(message, values));
}
