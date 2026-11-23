using System.Text.Json;
using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Transcripts;
using LouisAgent.Evals.Runs.Store;

namespace LouisAgent.Evals.Tests.Runs.Store;

/// <summary>
/// A run is kept as one file, named by its id: found again whole, replaced when saved again, listed by its eval
/// newest first, and read as strictly as every reader of ours.
/// </summary>
public class RunStoreTests
{
    private static readonly DateTimeOffset Started = new(2026, 11, 23, 4, 30, 12, TimeSpan.Zero);

    private string _folder = "";

    /// <summary>A runs folder of this test's own, not made yet.</summary>
    [SetUp]
    public void MakeFolder() =>
        _folder = Path.Combine(Path.GetTempPath(), "louis-agent-runs-" + Guid.NewGuid().ToString("N"), "runs");

    /// <summary>The folder goes when the test is done.</summary>
    [TearDown]
    public void RemoveFolder()
    {
        string parent = Path.GetDirectoryName(_folder)!;
        if (Directory.Exists(parent)) Directory.Delete(parent, recursive: true);
    }

    /// <summary>A saved run comes back as it was saved, every field and list included.</summary>
    [Test]
    public void ASavedRun_IsFoundAgainByItsId_AsItWasSaved()
    {
        var store = new RunStore(_folder);
        var run = Run("20261123-043012-guide", Started);

        store.Save(run);
        var found = store.Find(run.Id);

        Assert.That(Json(found!), Is.EqualTo(Json(run)));
    }

    /// <summary>Saving a run with the same id again replaces it.</summary>
    [Test]
    public void SavingTheSameIdAgain_ReplacesIt()
    {
        var store = new RunStore(_folder);
        var run = Run("20261123-043012-guide", Started);
        store.Save(run);

        store.Save(run with
        {
            Status = RunStatus.CouldNotFinish,
            StoppedBecause = "The judge gave no answer."
        });

        Assert.That(store.Find(run.Id)!.Status, Is.EqualTo(RunStatus.CouldNotFinish));
        Assert.That(Directory.GetFiles(_folder), Has.Length.EqualTo(1));
    }

    /// <summary>An eval's runs come back the newest first, and another eval's are not among them.</summary>
    [Test]
    public void Of_GivesThatEvalsRuns_NewestFirst()
    {
        var store = new RunStore(_folder);
        store.Save(Run("20261123-043012-guide", Started));
        store.Save(Run("20261124-090000-guide", Started.AddDays(1)) with { Eval = "guide" });
        store.Save(Run("20261123-050000-triage", Started) with { Eval = "triage" });

        var runs = store.Of("guide");

        Assert.That(runs.Select(run => run.Id), Is.EqualTo(new[] { "20261124-090000-guide", "20261123-043012-guide" }));
    }

    /// <summary>An id that isn't a run's id is refused before any path is built from it.</summary>
    [TestCase("../20261123-043012-guide")]
    [TestCase("20261123-043012-gu/ide")]
    [TestCase("20261123-043012-gu\\ide")]
    [TestCase("20261123-043012-..")]
    [TestCase("guide")]
    [TestCase("2026112-043012-guide")]
    [TestCase("20261123-043012-")]
    public void AnIdThatIsntARunsId_IsRefused(string id)
    {
        var store = new RunStore(_folder);

        var refused = Assert.Throws<ArgumentException>(() => store.Find(id));

        Assert.That(refused!.Message, Does.Contain(id));
        Assert.That(Directory.Exists(_folder), Is.False);
    }

    /// <summary>A run that isn't there is none.</summary>
    [Test]
    public void ARunThatIsntThere_IsNull() =>
        Assert.That(new RunStore(_folder).Find("20261123-043012-guide"), Is.Null);

    /// <summary>A file in the folder that isn't a run is an error that names the file.</summary>
    [Test]
    public void AFileThatIsntARun_IsAnErrorThatNamesTheFile()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Path.Combine(_folder, "20261123-043012-guide.json"), """{"id":"x","surprise":true}""");
        var store = new RunStore(_folder);

        var refused = Assert.Throws<InvalidDataException>(() => store.Of("guide"));

        Assert.That(refused!.Message, Does.Contain("20261123-043012-guide.json"));
    }

    /// <summary>Saving into a folder that isn't there yet makes it.</summary>
    [Test]
    public void SavingIntoAFolderThatIsntThereYet_MakesIt()
    {
        new RunStore(_folder).Save(Run("20261123-043012-guide", Started));

        Assert.That(File.Exists(Path.Combine(_folder, "20261123-043012-guide.json")), Is.True);
    }

    /// <summary>A run with every field filled: a turn with a tool call, a fact, a criterion, and a null cost.</summary>
    private static EvalRun Run(string id, DateTimeOffset started)
    {
        var call = new ToolCallTranscript("ReadDoc", """{"path":"docs/README.md"}""", "# Docs", Failed: true);
        var transcript = new TurnTranscript("What is louis-agent?", "A framework, as the [overview](/docs) says.",
            [call], TimeSpan.FromSeconds(7.5), "The model is overloaded.");
        var turn = new TurnRun(transcript, [new Mark("no-error", false, "Ended with an error.")],
            [new Mark("Says it is a framework", true, "It says so.")], null, null);
        var priced = new TurnRun(transcript with { Error = null }, [], [], 0.0042m, "USD");
        var conversation = new ConversationRun("guide-what-is", 2, [new RepeatRun(1, [turn, priced])]);
        return new EvalRun(id, "guide", "louis-agent-guide", RunKind.Replay, 2, started, [conversation])
        {
            Status = RunStatus.Finished,
            EndedAt = started.AddMinutes(3),
            StoppedBecause = "Not stopped, but every field is filled.",
            PageSent = true
        };
    }

    /// <summary>The run as its file holds it, so two runs compare by their lists' contents.</summary>
    private static string Json(EvalRun run) => JsonSerializer.Serialize(run, RunJson.Options);
}
