using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Tests.Datasets;

// The dataset loader (F18 step 3a): a folder of scenarios is read strictly, and refused whenever it can't be checked.
// Written first: the loader and the hash threw NotImplementedException on the red run.
public class DatasetLoaderTests
{
    private static readonly IReadOnlyList<IGrader> Graders = [new ReadsKeys("routed")];

    [Test]
    public void LoadsTheManifestAndBothSplits()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/cook-af.json", DatasetFolder.Scenario("cook-af", categories: "[\"language:af\"]"))
            .Write("scenarios/held-out/cook-held.json", DatasetFolder.Scenario("cook-held"))
            .WithManifest();

        Dataset dataset = DatasetLoader.Load(folder.Root, Graders);

        Assert.That(dataset.Manifest.Name, Is.EqualTo("puls-cook"));
        Assert.That(dataset.Manifest.Version, Is.EqualTo(1));
        Assert.That(dataset.Manifest.Covers, Is.EqualTo("Cooking meals"));
        Assert.That(dataset.Manifest.Changes, Is.EqualTo(new[] { "2026-10-24: first version" }));
        Assert.That(dataset.Scenarios.Select(s => (s.Id, s.Split)),
            Is.EqualTo(new[] { ("cook-af", DatasetSplit.Dev), ("cook-held", DatasetSplit.HeldOut) }));
        EvalScenario af = dataset.Scenarios[0];
        Assert.That(af.Categories, Is.EqualTo(new[] { "language:af" }));
        Assert.That(af.Input.Place, Is.EqualTo("Pretoria"));
        Assert.That(af.Input.TimeZone, Is.EqualTo("Africa/Johannesburg"));
        Assert.That(af.Input.Messages, Is.EqualTo(new[] { "Gee my resep" }));
        Assert.That(af.Expected.GetProperty("routed").GetBoolean(), Is.True);
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
    public void RefusesAFolderChangedWithoutANewVersion()
    {
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/a.json", DatasetFolder.Scenario("a"))
            .WithManifest();
        folder.Write("scenarios/dev/a.json", DatasetFolder.Scenario("a", expected: "{\"routed\": false}"));
        string hash = DatasetHash.Of(Path.Combine(folder.Root, "scenarios"));

        // The message names the hash to paste into dataset.json, once the version is bumped.
        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains(hash));
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
