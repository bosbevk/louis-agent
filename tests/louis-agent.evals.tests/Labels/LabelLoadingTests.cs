using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Labels;
using LouisAgent.Evals.Tests.Datasets;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Tests.Labels;

// F18 step 6.3: a person's labels of stored answers are part of the dataset, in `labels/*.json` beside `scenarios/` (the dataset's hash covers the scenarios only, so labelling
// changes no version). Each file says which answer it labels and Louis's verdict: `meal` true or false, and a note. A file with no label yet (`"label": null`) is refused with its
// name, so a half-labelled sheet can never be calibrated against as if it were whole. Written first: LabelLoader did not exist.
public class LabelLoadingTests
{
    private static readonly IReadOnlyList<IGrader> Graders = [new ReadsKeys("routed")];

    private static string Label(string scenario = "sample-dev", string label = "{\"meal\": true, \"note\": \"feeds four\"}", int turn = 1, string extra = "") =>
        "{\"scenario\": \"" + scenario + "\", \"mode\": \"puls-cook\", \"run\": \"8f7802b9a72e\", \"repeat\": 2, \"turn\": " + turn + ", \"answer\": \"Bobotie vir vier\", \"label\": "
        + label + extra + "}";

    private static DatasetFolder Folder() =>
        new DatasetFolder().Write("scenarios/dev/sample-dev.json", DatasetFolder.Scenario("sample-dev")).WithManifest();

    [Test]
    public void TheDatasetCarriesItsLabels_WithTheAnswerTheyLabelAndTheVerdict()
    {
        using var folder = Folder().Write("labels/a.json", Label()).Write("labels/b.json", Label(label: "{\"meal\": false, \"note\": \"a side\"}"));

        var labels = DatasetLoader.Load(folder.Root, Graders).Labels;

        Assert.That(labels.Select(l => (l.File, l.Scenario, l.Mode, l.Run, l.Repeat, l.Turn, l.Answer, l.Meal, l.Note)), Is.EqualTo(new[]
        {
            ("a.json", "sample-dev", "puls-cook", "8f7802b9a72e", 2, 1, "Bobotie vir vier", true, "feeds four"),
            ("b.json", "sample-dev", "puls-cook", "8f7802b9a72e", 2, 1, "Bobotie vir vier", false, "a side"),
        }));
    }

    [Test]
    public void ADatasetWithNoLabelsFolder_HasNoLabels()
    {
        using var folder = Folder();

        Assert.That(DatasetLoader.Load(folder.Root, Graders).Labels, Is.Empty);
    }

    [Test]
    public void ALabelNotGivenYet_IsRefusedWithItsFilesName()
    {
        using var folder = Folder().Write("labels/done.json", Label()).Write("labels/cook-af.puls-cook.8f7802b9a72e.1.json", Label(label: "null"));

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("cook-af.puls-cook.8f7802b9a72e.1.json").And.Message.Contains("no label"));
    }

    [Test]
    public void ALabelWithNoVerdict_IsNotGivenEither()
    {
        using var folder = Folder().Write("labels/a.json", Label(label: "{\"meal\": null, \"note\": \"unsure\"}"));

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders), Throws.TypeOf<InvalidDataException>().With.Message.Contains("a.json"));
    }

    [Test]
    public void ALabelOfAScenarioTheDatasetHasNot_IsRefused()
    {
        using var folder = Folder().Write("labels/a.json", Label(scenario: "gone"));

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("a.json").And.Message.Contains("gone"));
    }

    [Test]
    public void ALabelOfATurnTheScenarioHasNot_IsRefused()
    {
        using var folder = Folder().Write("labels/a.json", Label(turn: 3));

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders), Throws.TypeOf<InvalidDataException>().With.Message.Contains("a.json").And.Message.Contains("turn 3"));
    }

    [Test]
    public void AFileThatIsNotALabel_IsRefusedWithItsName()
    {
        using var folder = Folder().Write("labels/a.json", Label(extra: ", \"colour\": \"red\"")).Write("labels/b.json", "not json");

        var error = Assert.Throws<InvalidDataException>(() => DatasetLoader.Load(folder.Root, Graders))!;

        Assert.That(error.Message, Does.Contain("a.json").Or.Contain("b.json"));
    }

    [Test]
    public void LabellingChangesNoDatasetHash()
    {
        using var folder = Folder();
        string before = DatasetLoader.Load(folder.Root, Graders).Manifest.ContentHash;

        folder.Write("labels/a.json", Label());

        Assert.That(DatasetLoader.Load(folder.Root, Graders).Manifest.ContentHash, Is.EqualTo(before), "the labels are not scenarios: no version to bump");
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
