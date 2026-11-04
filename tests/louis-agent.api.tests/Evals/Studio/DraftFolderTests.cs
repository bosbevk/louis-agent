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
/// The folder is the draft's own, written afresh each time; an edit of an eval the workspace has starts from its
/// folder (FX32).
/// </summary>
public class DraftFolderTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 3, 10, 0, 0, TimeSpan.Zero);
    private static readonly ConversationKey CookAf = new(SampleMode.Beta, 1, "cook-af");

    /// <summary>Where the folders go in this test, and the workspace an edited eval is read from.</summary>
    private string _root = null!, _workspace = null!;

    /// <summary>A root and a workspace for every test; the workspace has no eval until a test writes one.</summary>
    [SetUp]
    public void MakeARoot()
    {
        string run = Path.Combine(Path.GetTempPath(), "louis-agent-draft-folder-" + Guid.NewGuid().ToString("N")[..8]);
        (_root, _workspace) = (Path.Combine(run, "drafts"), Path.Combine(run, "workspace"));
    }

    /// <summary>Both go when the test is done.</summary>
    [TearDown]
    public void DeleteTheRoot()
    {
        string run = Path.GetDirectoryName(_root)!;
        if (Directory.Exists(run)) Directory.Delete(run, recursive: true);
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
        string folder = DraftFolder.Write(Composed(), Drafts(), _root, null);

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
        string folder = DraftFolder.Write(Composed(), Drafts(), _root, null);

        var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "Dataset", "dataset.json")))
            .RootElement;
        Assert.That((manifest.GetProperty("name").GetString(), manifest.GetProperty("version").GetInt32(),
                manifest.GetProperty("content_hash").GetString(), manifest.GetProperty("changes").GetArrayLength()),
            Is.EqualTo(("weather-eval", 1, "", 1)));
    }

    /// <summary>
    /// An edit of an eval the workspace has (FX32) starts from its folder: what git holds and the studio doesn't (its
    /// references, held-out scenarios, labels, manual and manifest) stays, and the draft's own files go over it.
    /// </summary>
    [Test]
    public void AnEditOfAnEvalTheWorkspaceHas_KeepsItsFolder_UnderTheDraftsFiles()
    {
        // Named as the runner lists it, not after the eval: the cook's folder is Cook.
        string kept = Path.Combine(_workspace, "identities", "louis-agent.identity.weather-agent.evals", "Evals",
            "Weather");
        Write(kept, "manual.md", "# Weather");
        Write(kept, "Transcripts/weather-old.reference.txt", "{\"event\":\"mode\"}\n");
        Write(kept, "Dataset/dataset.json", """{"name":"weather-eval","version":4}""");
        Write(kept, "Dataset/scenarios/held-out/weather-worcester.json", "{}");
        Write(kept, "Rubrics/weather-right.md", "Was die weer reg?");

        // Only a kept transcript, so the scenarios are as git holds them and the manifest stays as it is.
        string folder = DraftFolder.Write(Composed(), new EvalDrafts([], Drafts().Transcripts), _root, kept);

        Assert.That(File.Exists(Path.Combine(folder, "manual.md")), Is.True, "the manual");
        Assert.That(File.Exists(Path.Combine(folder, "Transcripts", "weather-old.reference.txt")), Is.True,
            "the references git holds");
        Assert.That(File.Exists(Path.Combine(folder, "Dataset", "scenarios", "held-out", "weather-worcester.json")),
            Is.True, "the held-out scenarios");
        Assert.That(File.ReadAllText(Path.Combine(folder, "Dataset", "dataset.json")), Does.Contain("\"version\":4"),
            "the eval's own manifest, its version and changes");
        Assert.That(File.ReadAllText(Path.Combine(folder, "Rubrics", "weather-right.md")),
            Is.EqualTo("Is die weer reg?"), "the draft's rubric over the old one");
        Assert.That(File.Exists(Path.Combine(folder, "Transcripts", "weather-paarl.reference.txt")), Is.True,
            "and the kept transcript beside them");
    }

    /// <summary>
    /// An edit that changes the scenarios bumps the eval's manifest (FX32, Louis's pick): one version up, a dated
    /// change line, and the hash cleared for the runner to seal, as a person does by hand when a dataset changes.
    /// </summary>
    [Test]
    public void AnEditThatChangesTheScenarios_BumpsTheManifest()
    {
        string kept = AWorkspaceEval();

        string folder = DraftFolder.Write(Composed(), Drafts(), _root, kept);

        var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "Dataset", "dataset.json")))
            .RootElement;
        var changes = manifest.GetProperty("changes").EnumerateArray().Select(change => change.GetString()).ToList();
        Assert.That((manifest.GetProperty("version").GetInt32(), manifest.GetProperty("content_hash").GetString()),
            Is.EqualTo((2, "")));
        Assert.That(changes, Has.Count.EqualTo(2));
        Assert.That(changes[0], Is.EqualTo("2026-10-01: first version"));
        Assert.That(changes[1], Does.StartWith("2026-11-03: "));
        Assert.That(manifest.GetProperty("covers").GetString(), Is.EqualTo("Die weer in die Boland."),
            "the rest of the manifest as it was");
    }

    /// <summary>An edit whose scenarios are the ones git holds keeps the manifest as it is, hash and all.</summary>
    [Test]
    public void AnEditThatKeepsTheScenarios_KeepsTheManifest()
    {
        string kept = AWorkspaceEval();
        string sample = JsonSerializer.Serialize(Drafts().Samples[0].Sample, SampleBody.FileJson) + "\n";
        Write(kept, "Dataset/scenarios/dev/weather-paarl.json", sample);

        string folder = DraftFolder.Write(Composed(), Drafts(), _root, kept);

        Assert.That(File.ReadAllText(Path.Combine(folder, "Dataset", "dataset.json")),
            Is.EqualTo(File.ReadAllText(Path.Combine(kept, "Dataset", "dataset.json"))));
    }

    /// <summary>An eval in the workspace with a sealed manifest at version 1, and its folder.</summary>
    private string AWorkspaceEval()
    {
        string kept = Path.Combine(_workspace, "identities", "louis-agent.identity.weather-agent.evals", "Evals",
            "Weather");
        Write(kept, "Dataset/dataset.json", """
                                            {"name":"weather-eval","version":1,"covers":"Die weer in die Boland.",
                                             "content_hash":"abc123",
                                             "changes":["2026-10-01: first version"]}
                                            """);
        return kept;
    }

    /// <summary>Writing again starts afresh: a file the draft no longer has is gone.</summary>
    [Test]
    public void WritingAgain_StartsAfresh()
    {
        string first = DraftFolder.Write(Composed(), Drafts(), _root, null);
        string second = DraftFolder.Write(Composed() with { Rubrics = [] }, new EvalDrafts([], []), _root, null);

        Assert.That(second, Is.EqualTo(first));
        Assert.That(Directory.Exists(Path.Combine(second, "Rubrics")), Is.False);
        Assert.That(Directory.Exists(Path.Combine(second, "Transcripts")), Is.False);
    }

    /// <summary>Writes a file under a folder, making its folders.</summary>
    private static void Write(string folder, string path, string text)
    {
        string file = Path.Combine(folder, path);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, text);
    }
}
