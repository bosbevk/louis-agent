using LouisAgent.Api.Evals.Calibrations.Store;
using LouisAgent.Api.Evals.Reviews.Store;

namespace LouisAgent.Api.Evals.Store.Deletion;

/// <summary>
/// Deletes a run with everything it kept and everything people said about it: the eval store's part, then people's
/// grades and the judge's checks, which live in stores of their own (none when this instance has no Mongo for them).
/// A run that isn't there deletes nothing, and its grades are left alone.
/// </summary>
/// <remarks>
/// Extract Function (Fowler): the delete button's three calls moved here, so the start-up clean-up of old runs
/// deletes exactly what the button does and says how much of each went (Louis's pick).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0049-nothing-is-compatible-before-a-sale.md">ADR 0049: nothing is compatible before a sale</seealso>
public sealed class RunDeletion(IEvalStore store, IHumanReviewStore? reviews, ICalibrationStore? calibrations)
{
    /// <summary>Deletes the run and what was said about it, and says how much of each went.</summary>
    public async Task<DeletedRun> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var deleted = await store.DeleteRunAsync(id, cancellationToken);
        if (!deleted.Any) return deleted;

        // The run is gone, so what was said about it goes too.
        int grades = reviews is null ? 0 : await reviews.DeleteRunAsync(id, cancellationToken);
        int checks = calibrations is null ? 0 : await calibrations.DeleteRunAsync(id, cancellationToken);
        return deleted with
        {
            Grades = grades,
            Calibrations = checks
        };
    }
}
