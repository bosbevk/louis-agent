using LouisAgent.Api.Contracts.Admin.Evals;

namespace LouisAgent.Puls.Admin.Studio;

/// <summary>
/// The runs the Studio's try can replay, as a list to pick from (the admin review's finding 6: recognition rather than
/// recall, so nobody types a run id from memory).
/// </summary>
/// <remarks>
/// Presentation Model (Fowler): the picker's rows worked out in plain C# from the Evals page's own reply, so a test
/// reads them without a page. Only a run that recorded its answers can be replayed (FX28); the draft's own eval comes
/// first, then the newest. ADR 0046: the start is shown in UTC.
/// </remarks>
/// <seealso href="https://martinfowler.com/eaaDev/PresentationModel.html">Presentation Model</seealso>
public static class ReplayChoices
{
    /// <summary>Every recorded run of every eval, the eval named first, each newest first.</summary>
    public static IReadOnlyList<ReplayChoice> From(IEnumerable<EvalRow> evals, string? eval) =>
    [
        .. evals.SelectMany(row => row.Runs.Select(run => (Eval: row.Name, run.Run)))
            .Where(pair => pair.Run.Recorded)
            .OrderByDescending(pair => pair.Eval == eval)
            .ThenByDescending(pair => pair.Run.StartedAt)
            .Select(pair => new ReplayChoice(pair.Run.Id,
                StudioText.ReplayLabel(pair.Eval, pair.Run.StartedAt.UtcDateTime, pair.Run.Id)))
    ];
}
