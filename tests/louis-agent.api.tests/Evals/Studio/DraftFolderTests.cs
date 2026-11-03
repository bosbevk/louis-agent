using System.Text.Json;
using LouisAgent.Api.Evals.Drafts;
using LouisAgent.Api.Evals.Drafts.Composed;
using LouisAgent.Api.Evals.Drafts.Samples;
using LouisAgent.Api.Evals.Drafts.Transcripts;
using LouisAgent.Api.Evals.Studio;
using LouisAgent.Evals.Protocol.Parts.Conversations;

namespace LouisAgent.Api.Tests.Evals.Studio;

/// <summary>
/// An eval being composed, written out as the folder the runner reads (F49.5): its file as typed, its rubrics, a
/// manifest the runner seals, the eval's sample drafts as dev scenarios and its transcript drafts under their names.
/// The folder is the draft's own, written afresh each time.
/// </summary>
public class DraftFolderTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 3, 10, 0, 0, TimeSpan.Zero);
    private static readonly ConversationKey CookAf = new(SampleMode.Beta, 1, "cook-af");

    /// <summary>Where the folders go in this test.</summary>
    private string _root = null!;

    /// <summary>A root of its own for every test.</summary>
    [SetUp]
    public void MakeARoot() =>
        _root = Path.Combine(Path.GetTempPath(), "louis-agent-draft-folder-" + Guid.NewGuid().ToString("N")[..8]);

    /// <summary>The root goes when the test is done.</summary>
    [TearDown]
    public void DeleteTheRoot()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    /// <summary>The eval being composed.</summary>
    private static EvalDraft Composed() =>
        new("d1", "weather-agent", "weather-eval", """{"name":"weather-eval"}""",
            [new RubricFile("weather-right.md", "Is die weer reg?")], "Louis", At);

    /// <summary>One sample draft and one transcript draft of the eval.</summary>
    private static EvalDrafts Drafts() => new(
        [
            new SampleDraft("s1", "weather-eval", "weather-agent", "run1", CookAf,
                new SampleBody("weather-paarl", [new SampleBodyMessage("user", "Gaan dit reën?")],
                    new SampleBodyMetadata("Paarl", "Africa/Johannesburg", [],
                        JsonDocument.Parse("""{"rain":true}""").RootElement.Clone(), ["language:af"])), "Louis", At)
        ],
        [
            new TranscriptDraft("t1", "weather-eval", "weather-agent", "run1", CookAf, TranscriptKind.Reference, null,
                "weather-paarl.reference.txt", ["""{"event":"mode"}""", """{"event":"turn"}"""], "Louis", At)
        ]);

    /// <summary>Every file of the folder is there, as the runner reads it.</summary>
    [Test]
    public void TheFolder_HoldsTheFileTheRubricsTheScenariosAndTheTranscripts()
    {
        string folder = DraftFolder.Write(Composed(), Drafts(), _root);

        Assert.That(File.ReadAllText(Path.Combine(folder, "eval.json")), Is.EqualTo("""{"name":"weather-eval"}"""));
        Assert.That(File.ReadAllText(Path.Combine(folder, "Rubrics", "weather-right.md")),
            Is.EqualTo("Is die weer reg?"));
        var scenario = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "Dataset", "scenarios", "dev",
            "weather-paarl.json"))).RootElement;
        Assert.That(scenario.GetProperty("metadata").GetProperty("place").GetString(), Is.EqualTo("Paarl"));
        Assert.That(File.ReadAllText(Path.Combine(folder, "Transcripts", "weather-paarl.reference.txt")),
            Is.EqualTo("{\"event\":\"mode\"}\n{\"event\":\"turn\"}\n"));
    }

    /// <summary>The manifest names the eval's dataset and leaves its hash for the runner to seal.</summary>
    [Test]
    public void TheManifest_NamesTheDataset_AndLeavesItsHashToTheRunner()
    {
        string folder = DraftFolder.Write(Composed(), Drafts(), _root);

        var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "Dataset", "dataset.json")))
            .RootElement;
        Assert.That((manifest.GetProperty("name").GetString(), manifest.GetProperty("version").GetInt32(),
                manifest.GetProperty("content_hash").GetString(), manifest.GetProperty("changes").GetArrayLength()),
            Is.EqualTo(("weather-eval", 1, "", 1)));
    }

    /// <summary>Writing again starts afresh: a file the draft no longer has is gone.</summary>
    [Test]
    public void WritingAgain_StartsAfresh()
    {
        string first = DraftFolder.Write(Composed(), Drafts(), _root);
        string second = DraftFolder.Write(Composed() with { Rubrics = [] }, new EvalDrafts([], []), _root);

        Assert.That(second, Is.EqualTo(first));
        Assert.That(Directory.Exists(Path.Combine(second, "Rubrics")), Is.False);
        Assert.That(Directory.Exists(Path.Combine(second, "Transcripts")), Is.False);
    }
}
