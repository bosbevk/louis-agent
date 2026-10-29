using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Datasets.Manifest;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Tests.Datasets;

/// <summary>
/// The dataset loader (F18 step 3a): a folder of scenarios is read strictly, and refused whenever it can't be checked.
/// Each file is in Inspect's Sample shape (step 7d): <c>id</c>, <c>input</c> (the user's first question),
/// <c>target</c>, and ours under <c>metadata</c>.
/// </summary>
public class DatasetLoaderTests
{
    /// <summary>A grader that reads the one key the sample scenarios set.</summary>
    private static readonly IReadOnlyList<IGrader> Graders = [new ReadsKeys("routed")];

    /// <summary>The manifest's fields come through, and each split's scenarios with their input and criteria.</summary>
    [Test]
    public void LoadsTheManifestAndBothSplits()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/sample-dev.json",
                DatasetFolder.Scenario("sample-dev", categories: "[\"sample:first\"]"))
            .Write("scenarios/held-out/sample-held.json", DatasetFolder.Scenario("sample-held"))
            .WithManifest();

        Dataset dataset = DatasetLoader.Load(folder.Root, Graders);

        Assert.That(dataset.Manifest.Name, Is.EqualTo("sample-eval"));
        Assert.That(dataset.Manifest.Version, Is.EqualTo(1));
        Assert.That(dataset.Manifest.Covers, Is.EqualTo("A sample task"));
        Assert.That(dataset.Manifest.Changes, Is.EqualTo(new[] { "2026-10-24: a sample change" }));
        Assert.That(dataset.Scenarios.Select(s => (s.Id, s.Split)),
            Is.EqualTo(new[] { ("sample-dev", DatasetSplit.Dev), ("sample-held", DatasetSplit.HeldOut) }));
        EvalScenario dev = dataset.Scenarios[0];
        Assert.That(dev.Categories, Is.EqualTo(new[] { "sample:first" }));
        Assert.That(dev.Input.Place, Is.EqualTo("Sample town"));
        Assert.That(dev.Input.TimeZone, Is.EqualTo("Etc/UTC"));
        Assert.That(dev.Input.Messages, Is.EqualTo(new[] { "Hello" }));
        Assert.That(dev.Expected.GetProperty("routed").GetBoolean(), Is.True);
        Assert.That(dev.Target, Is.Null);
    }

    /// <summary>A sample's follow-ups are asked after its question, in order, and its target is kept.</summary>
    [Test]
    public void ASamplesFollowUpsComeAfterItsQuestion_AndItsTargetIsKept()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/a.json", DatasetFolder.Scenario("a", followUps: "[\"And for six?\", \"Thanks\"]",
                target: ", \"target\": \"a meal for six\""))
            .WithManifest();

        EvalScenario scenario = DatasetLoader.Load(folder.Root, Graders).Scenarios.Single();

        Assert.That(scenario.Input.Messages, Is.EqualTo(new[] { "Hello", "And for six?", "Thanks" }));
        Assert.That(scenario.Target, Is.EqualTo("a meal for six"));
    }

    /// <summary>A file still in the old scenario shape (an input object, criteria at the top) is refused.</summary>
    [Test]
    public void RefusesAFileInTheOldScenarioShape()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/a.json",
                "{\"id\": \"a\", \"input\": {\"place\": null, \"time_zone\": \"Etc/UTC\", \"messages\": [\"Hello\"]},"
                + " \"expected\": {\"routed\": true}, \"categories\": []}")
            .WithManifest();

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("input"));
    }

    /// <summary>
    /// The input is the user's first question, one message from the user; none, two, or one from the assistant is
    /// refused (follow-ups go in <c>metadata.follow_ups</c>).
    /// </summary>
    [TestCase("[]")]
    [TestCase("[{\"role\": \"user\", \"content\": \"A\"}, {\"role\": \"user\", \"content\": \"B\"}]")]
    [TestCase("[{\"role\": \"assistant\", \"content\": \"A\"}]")]
    public void RefusesAnInputThatIsNotOneQuestionFromTheUser(string input)
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/a.json",
                DatasetFolder.Scenario("a").Replace("[{\"role\": \"user\", \"content\": \"Hello\"}]", input))
            .WithManifest();

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("one question"));
    }

    /// <summary>Two scenarios with one id are refused, whichever split each is in.</summary>
    [Test]
    public void RefusesTwoScenariosWithOneId()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/a.json", DatasetFolder.Scenario("same"))
            .Write("scenarios/held-out/b.json", DatasetFolder.Scenario("same"))
            .WithManifest();

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("'same' twice"));
    }

    /// <summary>
    /// The split comes from the folder, so a scenario that names one itself is refused, as any unknown field is.
    /// </summary>
    [Test]
    public void RefusesAFieldTheLoaderDoesNotKnow()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/a.json", DatasetFolder.Scenario("a", extra: ", \"split\": \"dev\""))
            .WithManifest();

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("split"));
    }

    /// <summary>A criterion no grader reads would be checked by nobody, so it is refused.</summary>
    [Test]
    public void RefusesAnExpectedKeyNoGraderReads()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/a.json", DatasetFolder.Scenario("a", expected: "{\"shop\": true}"))
            .WithManifest();

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("'shop'"));
    }

    /// <summary>
    /// The hash catches a change to the scenarios; the message names both hashes, so the person sees what changed.
    /// </summary>
    [Test]
    public void RefusesAChangedFolderWhoseHashIsStale()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/a.json", DatasetFolder.Scenario("a"))
            .WithManifest();
        string recorded = DatasetHash.Of(Path.Combine(folder.Root, "scenarios"));
        folder.Write("scenarios/dev/a.json", DatasetFolder.Scenario("a", expected: "{\"routed\": false}"));
        string actual = DatasetHash.Of(Path.Combine(folder.Root, "scenarios"));

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains(actual).And.Message.Contains(recorded)
                .And.Message.Contains("If a scenario changed").And.Not.Message.Contains("without a new version"));
    }

    /// <summary>A split folder other than dev or held-out is refused by its name.</summary>
    [Test]
    public void RefusesAFolderThatIsNeitherDevNorHeldOut()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/training/a.json", DatasetFolder.Scenario("a"))
            .WithManifest();

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("'training'"));
    }

    /// <summary>The criteria must be an object of keys the graders read.</summary>
    [Test]
    public void RefusesAnExpectedThatIsNotAnObject()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/a.json", DatasetFolder.Scenario("a", expected: "\"routed\""))
            .WithManifest();

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("no object of success criteria"));
    }

    /// <summary>A folder with no <c>dataset.json</c> is refused.</summary>
    [Test]
    public void RefusesAFolderWithoutAManifest()
    {
        using var folder = new DatasetFolder().Write("scenarios/dev/a.json", DatasetFolder.Scenario("a"));

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("dataset.json"));
    }

    /// <summary>One dated line per version: a bump without its line fails.</summary>
    [Test]
    public void RefusesAVersionWithoutItsChangeLine()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/a.json", DatasetFolder.Scenario("a"))
            .WithManifest(version: 2, lines: 1);

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("needs 2"));
    }

    /// <summary>And a line without its bump fails too.</summary>
    [Test]
    public void RefusesAChangeLineWithoutItsVersion()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/a.json", DatasetFolder.Scenario("a"))
            .WithManifest(version: 1, lines: 2);

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("needs 1"));
    }

    /// <summary>A missing required field is refused, not bound to null: the run would fail far from the file.</summary>
    [Test]
    public void RefusesMetadataWithoutATimeZone()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/a.json",
                DatasetFolder.Scenario("a").Replace(" \"time_zone\": \"Etc/UTC\",", ""))
            .WithManifest();

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("time_zone"));
    }

    /// <summary>Categories may be left out, but a null is refused.</summary>
    [Test]
    public void RefusesCategoriesThatAreNull()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/a.json", DatasetFolder.Scenario("a", categories: "null"))
            .WithManifest();

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("categories"));
    }

    /// <summary>
    /// The hash covers every file under <c>scenarios/</c>, so the loader must read every one of them or refuse it.
    /// </summary>
    [Test]
    public void RefusesAScenarioOutsideTheSplits()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/a.json", DatasetFolder.Scenario("a"))
            .WithManifest();

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("'a.json'"));
    }

    /// <summary>A scenario in a folder inside a split is refused.</summary>
    [Test]
    public void RefusesAScenarioNestedInASplit()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/extra/a.json", DatasetFolder.Scenario("a"))
            .WithManifest();

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("'dev/extra/a.json'"));
    }

    /// <summary>A file that isn't a scenario's JSON is refused by its name.</summary>
    [Test]
    public void RefusesAFileThatIsNotScenarioJson()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/notes.txt", "not a scenario")
            .WithManifest();

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("'dev/notes.txt'"));
    }

    /// <summary>
    /// The same name on Windows and Linux: the extension is checked ordinally and in lowercase, never by the file
    /// system.
    /// </summary>
    [Test]
    public void RefusesAScenarioWhoseNameIsNotLowercaseJson()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/Cook-AF.JSON", DatasetFolder.Scenario("a"))
            .WithManifest();

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("'dev/Cook-AF.JSON'"));
    }

    /// <summary>
    /// A stray file (notes.txt, desktop.ini) is in the hash, so it also makes the hash stale. The file is the problem,
    /// so it is what the loader names: a version bump would only hash the stray file in.
    /// </summary>
    [Test]
    public void AStrayFileIsRefusedBeforeAHashChange()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/a.json", DatasetFolder.Scenario("a"))
            .WithManifest()
            .Write("scenarios/dev/desktop.ini", "stray");

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("'dev/desktop.ini'"));
    }

    /// <summary>
    /// A grader that reads a key no scenario sets would grade nothing, and say so nowhere (F18 step 5.4).
    /// </summary>
    [Test]
    public void RefusesAGraderKeyNoScenarioFeeds()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/a.json", DatasetFolder.Scenario("a"))
            .WithManifest();

        Assert.That(() => DatasetLoader.Load(folder.Root, [new ReadsKeys("routed", "budget")]),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("'budget'"));
    }

    /// <summary>A grader that reads no key needs no scenario to feed it.</summary>
    [Test]
    public void AGraderThatReadsNoKeyNeedsNoScenarioToFeedIt()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/a.json", DatasetFolder.Scenario("a", expected: "{}"))
            .WithManifest();

        Assert.That(DatasetLoader.Load(folder.Root, [new ReadsKeys()]).Scenarios, Has.Count.EqualTo(1));
    }

    /// <summary>A grader that is named only for the keys it reads; it never grades.</summary>
    private sealed class ReadsKeys(params string[] keys) : IGrader
    {
        /// <summary>Its name.</summary>
        public string Name => "reads-keys";

        /// <summary>Its version.</summary>
        public int Version => 1;

        /// <summary>Its kind.</summary>
        public GraderKind Kind => GraderKind.FixedRule;

        /// <summary>It is required.</summary>
        public bool Required => true;

        /// <summary>The keys it reads.</summary>
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>(keys);

        /// <summary>Never called: the loader only reads its keys.</summary>
        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }
}
