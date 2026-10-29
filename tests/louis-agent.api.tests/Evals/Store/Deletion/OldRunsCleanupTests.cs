using LouisAgent.Api.Evals.Calibrations;
using LouisAgent.Api.Evals.Calibrations.Store;
using LouisAgent.Api.Evals.Reviews;
using LouisAgent.Api.Evals.Reviews.Store;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Deletion;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using Microsoft.Extensions.Logging.Abstractions;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Tests.Evals.Store.Deletion;

/// <summary>
/// When the store opens, every run written under an older protocol version (or none) goes, with everything it kept
/// and everything people said about it; a run of this version stays. Stored eval data is disposable until a sale
/// (ADR 0049), so nothing older is read or kept.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0049-nothing-is-compatible-before-a-sale.md">ADR 0049: nothing is compatible before a sale</seealso>
public class OldRunsCleanupTests
{
    /// <summary>When everything happened.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 28, 9, 0, 0, TimeSpan.Zero);

    /// <summary>The conversation everything is about.</summary>
    private static readonly ConversationKey Bobotie = new(SampleMode.Alpha, 1, "bobotie");

    /// <summary>The eval store and the stores of what people said, together.</summary>
    private sealed record Stores(
        InMemoryEvalStore Evals,
        InMemoryHumanReviewStore Reviews,
        InMemoryCalibrationStore Calibrations);

    /// <summary>
    /// Stores holding an old run (no protocol version) and a current one (version 2), each with a conversation, a
    /// grade and a judge check.
    /// </summary>
    private static async Task<Stores> HoldingAnOldAndACurrentRunAsync()
    {
        var stores = new Stores(new InMemoryEvalStore(), new InMemoryHumanReviewStore(),
            new InMemoryCalibrationStore());
        foreach (var (run, version) in new[] { ("old", (int?)null), ("current", 2) })
        {
            await stores.Evals.SaveRunAsync(
                new Run(run, "check-x", 1, EvalRunStatus.Done, T0) { ProtocolVersion = version });
            await stores.Evals.SaveConversationAsync(run, new Conversation(Bobotie, 1, null, null, 0, 1));
            await stores.Reviews.AddAsync(new HumanGrade(run, Bobotie, "meal", HumanVerdict.Pass, "ok", "a", T0));
            await stores.Calibrations.AddAsync(new CalibrationRecord(run, "meal", null, T0, 1, 1, 1, true,
                new CalibrationClassCounts(1, 0, 0), new CalibrationClassCounts(0, 0, 0), []));
        }

        return stores;
    }

    /// <summary>The clean-up over these stores, logging nowhere.</summary>
    private static OldRunsCleanup CleanupOf(Stores stores) =>
        new(stores.Evals, new RunDeletion(stores.Evals, stores.Reviews, stores.Calibrations), NullLogger.Instance);

    /// <summary>The old run and what was said about it go, counted; the current run keeps all of its own.</summary>
    [Test]
    public async Task TheOldRun_Goes_WithWhatWasSaidAboutIt_AndTheCurrentOneStays()
    {
        var stores = await HoldingAnOldAndACurrentRunAsync();

        var deleted = await CleanupOf(stores).RunAsync();

        Assert.That(deleted, Is.EqualTo(new DeletedRun(1, 1, 0, 0, 0, Grades: 1, Calibrations: 1)));
        Assert.That(await stores.Evals.GetRunAsync("old"), Is.Null);
        Assert.That(await stores.Evals.GetRunAsync("current"), Is.Not.Null);
        Assert.That(await stores.Reviews.OfRunAsync("current"), Has.Count.EqualTo(1), "the current run's grade");
        Assert.That(await stores.Calibrations.OfRunAsync("current"), Has.Count.EqualTo(1), "its judge check");
    }

    /// <summary>Once the old runs are gone, opening the store again deletes nothing.</summary>
    [Test]
    public async Task ASecondCleanup_FindsNothing()
    {
        var stores = await HoldingAnOldAndACurrentRunAsync();
        await CleanupOf(stores).RunAsync();

        var again = await CleanupOf(stores).RunAsync();

        Assert.That(again.Any, Is.False);
    }
}
