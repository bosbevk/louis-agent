using LouisAgent.Api.Evals.Calibrations;
using LouisAgent.Api.Evals.Calibrations.Store;
using LouisAgent.Api.Evals.Reviews;
using LouisAgent.Api.Evals.Reviews.Store;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Deletion;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Tests.Evals.Store.Deletion;

/// <summary>
/// Deleting a run takes everything it kept and everything people said about it (their grades, the judge's checks),
/// and says how much of each went. The delete button and the start-up clean-up both go through it.
/// </summary>
public class RunDeletionTests
{
    /// <summary>When everything happened.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 28, 9, 0, 0, TimeSpan.Zero);

    /// <summary>The conversation everything is about.</summary>
    private static readonly ConversationKey Bobotie = new(SampleMode.Alpha, 1, "bobotie");

    /// <summary>The three stores, each holding a run with one conversation, one grade and one judge check.</summary>
    private sealed record Stores(
        InMemoryEvalStore Evals,
        InMemoryHumanReviewStore Reviews,
        InMemoryCalibrationStore Calibrations);

    /// <summary>Stores that hold everything about this run, and the same about another run.</summary>
    private static async Task<Stores> HoldingAsync(params string[] runs)
    {
        var stores = new Stores(new InMemoryEvalStore(), new InMemoryHumanReviewStore(),
            new InMemoryCalibrationStore());
        foreach (string run in runs)
        {
            await stores.Evals.SaveRunAsync(new Run(run, "check-x", 1, EvalRunStatus.Done, T0));
            await stores.Evals.SaveConversationAsync(run, new Conversation(Bobotie, 1, null, null, 0, 1));
            await stores.Reviews.AddAsync(new HumanGrade(run, Bobotie, "meal", HumanVerdict.Pass, "ok", "a", T0));
            await stores.Calibrations.AddAsync(new CalibrationRecord(run, "meal", null, T0, 1, 1, 1, true,
                new CalibrationClassCounts(1, 0, 0), new CalibrationClassCounts(0, 0, 0), []));
        }

        return stores;
    }

    /// <summary>
    /// The run, its conversation, its grade and its judge check all go, counted, and another run stays.
    /// </summary>
    [Test]
    public async Task DeletingARun_TakesWhatPeopleSaidAboutItToo_AndCountsIt()
    {
        var stores = await HoldingAsync("run1", "run2");

        var deleted = await new RunDeletion(stores.Evals, stores.Reviews, stores.Calibrations).DeleteAsync("run1");

        Assert.That(deleted, Is.EqualTo(new DeletedRun(1, 1, 0, 0, 0, Grades: 1, Calibrations: 1)));
        Assert.That(await stores.Reviews.OfRunAsync("run1"), Is.Empty);
        Assert.That(await stores.Calibrations.OfRunAsync("run1"), Is.Empty);
        Assert.That(await stores.Reviews.OfRunAsync("run2"), Has.Count.EqualTo(1), "another run keeps its grades");
    }

    /// <summary>A run that isn't there deletes nothing, and says so.</summary>
    [Test]
    public async Task AnUnknownRun_DeletesNothing()
    {
        var stores = await HoldingAsync("run1");

        var deleted = await new RunDeletion(stores.Evals, stores.Reviews, stores.Calibrations).DeleteAsync("nope");

        Assert.That(deleted.Any, Is.False);
        Assert.That(await stores.Reviews.OfRunAsync("run1"), Has.Count.EqualTo(1));
    }

    /// <summary>
    /// Without a review or calibration store (no Mongo for them), the run still goes and they count none.
    /// </summary>
    [Test]
    public async Task WithoutReviewOrCalibrationStores_TheRunStillGoes()
    {
        var stores = await HoldingAsync("run1");

        var deleted = await new RunDeletion(stores.Evals, null, null).DeleteAsync("run1");

        Assert.That(deleted, Is.EqualTo(new DeletedRun(1, 1, 0, 0, 0)));
    }
}
