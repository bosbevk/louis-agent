using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Rates;

namespace LouisAgent.Puls.Admin.Evals;

/// <summary>
/// What an eval's card shows: its newest run up top, the runs before it as a timeline, and its graders grouped by
/// how they judge. An eval that never ran has no newest run.
/// </summary>
/// <remarks>
/// Presentation Model (Fowler), chosen in CL26: the choices the card makes (which run leads, whether a run was a
/// replay or a re-score, how the graders group) are worked out here, in a plain class with NUnit tests, so the
/// components only place what they're given and no logic hides in the markup.
/// </remarks>
public sealed record EvalCardView(RunLine? Latest, IReadOnlyList<RunLine> Runs, IReadOnlyList<GraderGroup> Graders)
{
    /// <summary>Works out the card for one eval as the API listed it, its runs newest first.</summary>
    public static EvalCardView Of(EvalRow eval)
    {
        // Every run with where it came from and its modes' rates, still newest first.
        var lines = eval.Runs.Select(Line).ToList();

        // The graders by how they judge; GroupBy keeps each kind where it first came.
        var graders = eval.Graders.GroupBy(g => EvalText.Kind(g.Kind))
            .Select(group => new GraderGroup(group.Key, [.. group])).ToList();

        // The newest run leads the card; the timeline holds the ones before it.
        return new EvalCardView(lines.FirstOrDefault(), [.. lines.Skip(1)], graders);
    }

    /// <summary>One run with where its results came from, and each mode's last rates.</summary>
    private static RunLine Line(EvalRunRow run) =>
        new(run, Origin(run.Run), run.Run.RescoreOf ?? run.Run.ReplayOf, ModeRatesReader.Read(run.Modes));

    /// <summary>A run that names a run it re-scored is a re-score, one that names a recording is a replay.</summary>
    /// <remarks>A switch on the row's shape, one arm per origin, read like a table.</remarks>
    private static RunOrigin Origin(StoredRunRow run) => run switch
    {
        { RescoreOf: not null } => RunOrigin.Rescore,
        { ReplayOf: not null } => RunOrigin.Replay,
        _ => RunOrigin.Recorded
    };
}
