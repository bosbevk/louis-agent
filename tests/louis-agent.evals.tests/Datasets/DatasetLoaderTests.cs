using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Tests.Datasets;

// The dataset loader (F18 step 3a): a folder of scenarios is read strictly, and refused whenever it can't be checked.
// Written first: the loader and the hash threw NotImplementedException on the red run; each finding of the 3a.1 review
// was a red test here before its fix.
public class DatasetLoaderTests
{
    private static readonly IReadOnlyList<IGrader> Graders = [new ReadsKeys("routed")];

    [Test]
    public void LoadsTheManifestAndBothSplits()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/sample-dev.json", DatasetFolder.Scenario("sample-dev", categories: "[\"sample:first\"]"))
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
    }

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

    [Test]
    public void RefusesAFieldTheLoaderDoesNotKnow()
    {
        // The split comes from the folder, so a scenario that names one itself is refused, as any unknown field is.
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/a.json", DatasetFolder.Scenario("a", extra: ", \"split\": \"dev\""))
            .WithManifest();

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("split"));
    }

    [Test]
    public void RefusesAnExpectedKeyNoGraderReads()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/a.json", DatasetFolder.Scenario("a", expected: "{\"shop\": true}"))
            .WithManifest();

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("'shop'"));
    }

    [Test]
    public void RefusesAChangedFolderWhoseHashIsStale()
    {
        // The hash catches a change to the scenarios; the message names both hashes, so the person sees what changed.
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/a.json", DatasetFolder.Scenario("a"))
            .WithManifest();
        string recorded = DatasetHash.Of(Path.Combine(folder.Root, "scenarios"));
        folder.Write("scenarios/dev/a.json", DatasetFolder.Scenario("a", expected: "{\"routed\": false}"));
        string actual = DatasetHash.Of(Path.Combine(folder.Root, "scenarios"));

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains(actual).And.Message.Contains(recorded));
    }

    [Test]
    public void RefusesAFolderThatIsNeitherDevNorHeldOut()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/training/a.json", DatasetFolder.Scenario("a"))
            .WithManifest();

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("'training'"));
    }

    [Test]
    public void RefusesAnExpectedThatIsNotAnObject()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/a.json", DatasetFolder.Scenario("a", expected: "\"routed\""))
            .WithManifest();

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("no object of success criteria"));
    }

    [Test]
    public void RefusesAFolderWithoutAManifest()
    {
        using var folder = new DatasetFolder().Write("scenarios/dev/a.json", DatasetFolder.Scenario("a"));

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("dataset.json"));
    }

    [Test]
    public void RefusesAVersionWithoutItsChangeLine()
    {
        // One dated line per version: a bump without its line fails...
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/a.json", DatasetFolder.Scenario("a"))
            .WithManifest(version: 2, lines: 1);

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("needs 2"));
    }

    [Test]
    public void RefusesAChangeLineWithoutItsVersion()
    {
        // ...and a line without its bump fails too.
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/a.json", DatasetFolder.Scenario("a"))
            .WithManifest(version: 1, lines: 2);

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("needs 1"));
    }

    [Test]
    public void RefusesAnInputWithoutATimeZone()
    {
        // A missing constructor parameter is refused, not bound to null: the run would fail far from the file.
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/a.json", "{\"id\": \"a\", \"input\": {\"place\": null, \"messages\": [\"Hello\"]}, \"expected\": {\"routed\": true}, \"categories\": []}")
            .WithManifest();

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("time_zone"));
    }

    [Test]
    public void RefusesCategoriesThatAreNull()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/a.json", DatasetFolder.Scenario("a", categories: "null"))
            .WithManifest();

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("categories"));
    }

    [Test]
    public void RefusesAScenarioOutsideTheSplits()
    {
        // The hash covers every file under scenarios/, so the loader must read every one of them or refuse it.
        using var folder = new DatasetFolder()
            .Write("scenarios/a.json", DatasetFolder.Scenario("a"))
            .WithManifest();

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("'a.json'"));
    }

    [Test]
    public void RefusesAScenarioNestedInASplit()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/extra/a.json", DatasetFolder.Scenario("a"))
            .WithManifest();

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("'dev/extra/a.json'"));
    }

    [Test]
    public void RefusesAFileThatIsNotScenarioJson()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/notes.txt", "not a scenario")
            .WithManifest();

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("'dev/notes.txt'"));
    }

    [Test]
    public void RefusesAScenarioWhoseNameIsNotLowercaseJson()
    {
        // The same name on Windows and Linux: the extension is checked ordinally and in lowercase, never by the file system.
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/Cook-AF.JSON", DatasetFolder.Scenario("a"))
            .WithManifest();

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("'dev/Cook-AF.JSON'"));
    }

    /// <summary>A grader that is named only for the keys it reads; it never grades.</summary>
    private sealed class ReadsKeys(params string[] keys) : IGrader
    {
        public string Name => "reads-keys";
        public int Version => 1;
        public GraderKind Kind => GraderKind.FixedRule;
        public bool Required => true;
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>(keys);
        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
