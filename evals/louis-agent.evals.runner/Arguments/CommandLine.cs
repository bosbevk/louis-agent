using System.Globalization;
using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Runner.Messages;

namespace LouisAgent.Evals.Runner.Arguments;

/// <summary>What the command line asks for.</summary>
public abstract record CommandLineRequest;

/// <summary><c>--list</c>: the catalogue, one JSON object per line.</summary>
public sealed record ListRequest : CommandLineRequest;

/// <summary>
/// <c>run {name} [--repeat N] [--web off|record|replay] [--of &lt;runId&gt;] [--replay &lt;runId&gt;] [--split dev|held-out|all] | --rescore &lt;runId&gt;</c>: one eval, each mode repeated N times,
/// its web input recorded or replayed (F18 step 4a). A null <paramref name="Web"/> sends no choice, so the instance's <c>WEB_REPLAY</c>
/// decides; <paramref name="Of"/> is the recorded run's id, and is set only with a replay. <c>--replay {runId}</c> is a replay of that run
/// with <paramref name="Model"/> set: the model's answers come from the recording too (step 4e), so it costs nothing.
/// <paramref name="RescoreOf"/> (F18 step 5b, <c>--rescore {runId}</c>) re-grades that stored run's transcripts, read from stdin, and opens no session.
/// </summary>
public sealed record RunRequest(string Name, int Repeat, WebReplayMode? Web = null, string? Of = null, bool Model = false,
    SplitSelection Split = SplitSelection.Dev, string? RescoreOf = null) : CommandLineRequest;

/// <summary>The command line: <c>--list</c>, or <c>run {name}</c> and its flags. A refusal is a <see cref="RunnerException"/>.</summary>
public static class CommandLine
{
    /// <summary>
    /// Repeats one run may ask for, clamped to 1..MaxRepeat as the API's BenchmarkRunner clamps them. The API can't reference the
    /// runner, so this constant is its own: the two must match (src/louis-agent.api/Benchmarks/Runs/BenchmarkRunner.cs).
    /// </summary>
    public const int MaxRepeat = 5;

    public static CommandLineRequest Parse(IReadOnlyList<string> args)
    {
        if (args is ["--list"]) return new ListRequest();
        if (args is ["run", var name, ..]) return ParseRun(name, args.Skip(2).ToList());
        if (args.Count == 0 || args[0] == "run") throw Refuse(RunnerMessage.Usage, null);
        throw Refuse(RunnerMessage.UnknownArgument, new { Argument = args[0] });
    }

    /// <summary>The flags after a run's name, in any order; a flag given twice keeps its last value. Each flag has its own parser.</summary>
    private static RunRequest ParseRun(string name, IReadOnlyList<string> rest)
    {
        int repeat = 1;
        WebReplayMode? web = null;
        string? of = null, replay = null, rescore = null;
        var split = SplitSelection.Dev;
        // What shapes a session or a run, which a re-score has none of: given at all, whatever its value (F18 step 5b).
        bool shapesARun = false;
        for (int i = 0; i < rest.Count; i++)
        {
            string flag = rest[i];
            if (i + 1 >= rest.Count) throw Refuse(RunnerMessage.UnknownArgument, new { Argument = flag });
            string value = rest[++i];
            switch (flag)
            {
                case "--repeat": repeat = ParseRepeat(value); shapesARun = true; break;
                case "--web": web = ParseWeb(value); shapesARun = true; break;
                case "--of": of = ParseOf(value); break;
                case "--replay": replay = ParseOf(value); shapesARun = true; break;
                case "--split": split = ParseSplit(value); shapesARun = true; break;
                case "--rescore": rescore = ParseOf(value); break;
                default: throw Refuse(RunnerMessage.UnknownArgument, new { Argument = flag });
            }
        }
        if (rescore is not null)
        {
            if (shapesARun || of is not null) throw Refuse(RunnerMessage.RescoreExcludesRunFlags, null);
            return new RunRequest(name, repeat, RescoreOf: rescore);
        }
        if (replay is not null)
        {
            if (web is not null || of is not null) throw Refuse(RunnerMessage.ReplayExcludesWebAndOf, null);
            return new RunRequest(name, repeat, WebReplayMode.Replay, replay, Model: true, Split: split);
        }
        if (web == WebReplayMode.Replay && of is null) throw Refuse(RunnerMessage.ReplayNeedsOf, null);
        if (web != WebReplayMode.Replay && of is not null) throw Refuse(RunnerMessage.OfNeedsReplay, null);
        return new RunRequest(name, repeat, web, of, Split: split);
    }

    private static int ParseRepeat(string value)
    {
        if (!int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int n))
            throw Refuse(RunnerMessage.BadRepeat, new { Value = value });
        return Math.Clamp(n, 1, MaxRepeat);
    }

    /// <summary>The web choice, parsed once with <c>SettingChoice</c> (ADR 0024): the name with the case ignored, never a number.</summary>
    private static WebReplayMode ParseWeb(string value)
    {
        if (!SettingChoice.TryParse(value, out WebReplayMode? web) || web is null)
            throw Refuse(RunnerMessage.UnknownWeb, new { Value = value, Supported = string.Join(", ", SettingChoice.Names<WebReplayMode>()) });
        return web.Value;
    }

    /// <summary>The split, parsed once with <c>SettingChoice</c> (ADR 0024): <c>dev</c>, <c>held-out</c> (or <c>heldout</c>) or <c>all</c>, never a number.</summary>
    private static SplitSelection ParseSplit(string value)
    {
        if (!SettingChoice.TryParse(value, out SplitSelection? split, SplitAliases) || split is null)
            throw Refuse(RunnerMessage.UnknownSplit, new { Value = value, Supported = string.Join(", ", SettingChoice.Names<SplitSelection>()) });
        return split.Value;
    }

    private static readonly IReadOnlyDictionary<string, SplitSelection> SplitAliases = new Dictionary<string, SplitSelection> { ["held-out"] = SplitSelection.HeldOut };

    /// <summary>A run id's characters only, <c>[A-Za-z0-9_-]</c>: the API writes the trial's key from it, so it never carries a separator.</summary>
    private static string ParseOf(string value)
    {
        if (value.Length == 0 || !value.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-'))
            throw Refuse(RunnerMessage.BadOf, new { Value = value });
        return value;
    }

    private static RunnerException Refuse(RunnerMessage message, object? values) => new(MessageText.Format(message, values));
}
