using System.Globalization;
using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Choices;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Puls.Admin.Rates;
using LouisAgent.Puls.Admin.Runs;

namespace LouisAgent.Puls.Admin.Evals;

/// <summary>
/// How the eval list words an eval: whose it is, its version, dataset and graders, its modes' rates, where a run's
/// results came from and when it started, and whether it ran; and the share a rate's ring draws. Plain values, so
/// the page's components only place them.
/// </summary>
/// <remarks>Each choice of words is a switch on its shape, one arm per case, never nested ternaries.</remarks>
public static class EvalText
{
    /// <summary>
    /// How many runs it has, or that it never ran. Without a store nobody knows, and the words say that instead.
    /// </summary>
    public static string Runs(bool? neverRun, int runs) => (neverRun, runs) switch
    {
        (null, _) => "runs not known: no store",
        (true, _) => "never run",
        (_, 1) => "1 run",
        _ => $"{runs} runs"
    };

    /// <summary>The identity it proves; an eval of none, as the stub, belongs to the engine itself.</summary>
    public static string Whose(string? identity) => identity ?? "the engine's own";

    /// <summary>"v3"; a version we weren't told is a question mark.</summary>
    public static string Version(int? version) => version is { } v ? $"v{v}" : "v?";

    /// <summary>The dataset's name and version, or "none".</summary>
    public static string Dataset(EvalDatasetRow? dataset) =>
        dataset is { } d ? $"{d.Name} v{d.Version}" : "none";

    /// <summary>Names joined with commas, or a dash when there are none.</summary>
    public static string Names(IReadOnlyList<string> names) => names.Count == 0 ? "—" : string.Join(", ", names);

    /// <summary>A run's tag: recorded, or which run it replayed or re-scored, by its short id.</summary>
    public static string Origin(RunOrigin origin, string? source) => origin switch
    {
        RunOrigin.Replay => $"replay of {RunText.Short(source ?? "")}",
        RunOrigin.Rescore => $"re-scored from {RunText.Short(source ?? "")}",
        _ => "recorded"
    };

    /// <summary>
    /// A run tag's colour class: a recording, a replay or a re-score. Prefixed, since <c>.replay</c> is the landing
    /// page's button.
    /// </summary>
    public static string OriginTone(RunOrigin origin) => origin switch
    {
        RunOrigin.Replay => "is-replay",
        RunOrigin.Rescore => "is-rescore",
        _ => "is-recorded"
    };

    /// <summary>What a run's tag means, on hover: only a recording asked the model.</summary>
    public static string OriginHint(RunOrigin origin) => origin switch
    {
        RunOrigin.Replay =>
            "The recording's answers played back: no model was asked, so it costs nothing and measures nothing new.",
        RunOrigin.Rescore => "That run's transcripts graded again with today's graders. No model was asked.",
        _ => "The model answered live."
    };

    /// <summary>
    /// A rate's ring: the share passed and the rest. With no rate both are 0, so only the empty track shows.
    /// </summary>
    public static IReadOnlyList<double> Ring(double? rate) => rate is { } r ? [r, 1 - r] : [0, 0];

    /// <summary>The fold's note: how many graders, categories and modes the eval has.</summary>
    public static string Checks(int graders, int categories, int modes) =>
        $"{Count(graders, "grader")} · {Count(categories, "category", "categories")} · {Count(modes, "mode")}";

    /// <summary>A grader kind's colour on the card: rules, a model judge and a person each have their own.</summary>
    public static string Tone(string kind) =>
        (SettingChoice.TryParse(kind, out CheckKind? known) ? known : null) switch
        {
            CheckKind.FixedRule => "rule",
            CheckKind.Model => "model",
            CheckKind.Human => "person",
            _ => "other"
        };

    /// <summary>When a run started, as a day and a time in the reader's own zone.</summary>
    public static string When(DateTimeOffset at) =>
        at.ToLocalTime().ToString("d MMM yyyy, HH:mm", CultureInfo.InvariantCulture);

    /// <summary>"1 grader" or "9 graders"; a word whose plural isn't just an s says so.</summary>
    private static string Count(int count, string one, string? many = null) =>
        count == 1 ? $"1 {one}" : $"{count} {many ?? one + "s"}";

    /// <summary>What a grader judges with, in words; a kind we don't know is shown as it came.</summary>
    public static string Kind(string kind) =>
        (SettingChoice.TryParse(kind, out CheckKind? known) ? known : null) switch
        {
            CheckKind.FixedRule => "fixed rule",
            CheckKind.Model => "model",
            CheckKind.Human => "a person",
            _ => kind
        };
}
