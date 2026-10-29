using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Choices;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Puls.Admin.Rates;

namespace LouisAgent.Puls.Admin.Evals;

/// <summary>
/// How the eval list words an eval: whose it is, its version, dataset and graders, its modes' rates, and whether it
/// ran. Plain text, so the page's components only place it.
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

    /// <summary>A grader's name and version, and in words what it judges with.</summary>
    public static string Grader(EvalGraderRow grader) =>
        $"{grader.Name} v{grader.Version} · {Kind(grader.Kind)}";

    /// <summary>Names joined with commas, or a dash when there are none.</summary>
    public static string Names(IReadOnlyList<string> names) => names.Count == 0 ? "—" : string.Join(", ", names);

    /// <summary>A mode's pass@k and pass^k, as the runs page writes them.</summary>
    public static string Rates(ModeRow mode) =>
        $"{mode.Name} {RatesText.PassAt(mode.K)} {RatesText.Share(mode.PassAtK)} · "
        + $"{RatesText.PassHat(mode.K)} {RatesText.Share(mode.PassHatK)}";

    /// <summary>What a grader judges with, in words; a kind we don't know is shown as it came.</summary>
    public static string Kind(string kind) =>
        (SettingChoice.TryParse(kind, out GraderKind? known) ? known : null) switch
        {
            GraderKind.FixedRule => "fixed rule",
            GraderKind.Model => "model",
            GraderKind.Human => "a person",
            _ => kind
        };
}
