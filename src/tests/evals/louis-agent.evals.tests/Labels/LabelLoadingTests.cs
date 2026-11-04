using LouisAgent.Testing;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Labels;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Tests.Datasets;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Tests.Labels;

// F18 step 6.3: a person's labels of stored answers are part of the dataset, in `labels/*.json` beside `scenarios/` (the dataset's hash covers the scenarios only, so labelling
// changes no version). Each file says which answer it labels and Louis's verdict: `meal` true or false, and a note. A file with no label yet (`"label": null`) is refused with its
// name, so a half-labelled sheet can never be calibrated against as if it were whole. Written first: LabelLoader did not exist.
public class LabelLoadingTests
{
    private static readonly IReadOnlyList<ICheck> Graders = [new ReadsKeys("routed")];

    // 6a.4f (Louis): a label carries the conversation's turns, every question and answer, so a judge can see what a follow-up relies on; the last turn's answer is the one labelled.
    private static string Label(string scenario = "sample-dev",
        string label = "{\"pass\": true, \"note\": \"feeds four\"}", int turn = 1, string extra = "",
        string grader = "reads-keys") =>
        "{\"grader\": \"" + grader + "\", \"scenario\": \"" + scenario +
        SampleMode.Fill("\", \"mode\": \"<ModeBeta>\", \"run\": \"8f7802b9a72e\", \"repeat\": 2, \"turns\": [")
        + string.Join(", ",
            Enumerable.Range(1, turn).Select(n =>
                n == turn
                    ? "{\"question\": \"Vir vier\", \"answer\": \"Bobotie vir vier\"}"
                    : "{\"question\": \"Hallo\", \"answer\": \"Hallo!\"}"))
        + "], \"label\": " + label + extra + "}";

    private static DatasetFolder Folder() =>
        new DatasetFolder().Write("scenarios/dev/sample-dev.json", DatasetFolder.Scenario("sample-dev")).WithManifest();

    [Test]
    public void TheDatasetCarriesItsLabels_WithTheAnswerTheyLabelAndTheVerdict()
    {
        using var folder = Folder().Write("labels/a.json", Label())
            .Write("labels/b.json", Label(label: "{\"pass\": false, \"note\": \"a side\"}"));

        var labels = DatasetLoader.Load(folder.Root, Graders).Labels;

        Assert.That(labels[0].Turns, Is.EqualTo(new[] { new LabelTurn("Vir vier", "Bobotie vir vier") }));
        Assert.That(
            labels.Select(l =>
                (l.File, l.Grader, l.Scenario, l.Mode, l.Run, l.Repeat, l.Turn, l.Answer, l.Pass, l.Note)), Is.EqualTo(
                new[]
                {
                    ("a.json", "reads-keys", "sample-dev", SampleMode.Beta, "8f7802b9a72e", 2, 1, "Bobotie vir vier",
                        true, "feeds four"),
                    ("b.json", "reads-keys", "sample-dev", SampleMode.Beta, "8f7802b9a72e", 2, 1, "Bobotie vir vier",
                        false, "a side"),
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
        using var folder = Folder().Write("labels/done.json", Label()).Write(
            SampleScenario.Fill("labels/<ScenarioAlpha>.puls-cook.8f7802b9a72e.1.json"), Label(label: "null"));

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message
                .Contains(SampleScenario.Fill("<ScenarioAlpha>.puls-cook.8f7802b9a72e.1.json")).And.Message
                .Contains("no label"));
    }

    [Test]
    public void ALabelWithNoVerdict_IsNotGivenEither()
    {
        using var folder = Folder().Write("labels/a.json", Label(label: "{\"pass\": null, \"note\": \"unsure\"}"));

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("a.json"));
    }

    // F18 step 6a.0 (the 6.3 review): a label is generic, a grader and pass true or false, so the next judge (a gardener's, say) can reuse the files' shape and a human grade can export to it.
    // A label of a grader the eval does not have would calibrate nothing, so it is refused.
    [Test]
    public void ALabelOfAGraderTheEvalHasNot_IsRefused()
    {
        using var folder = Folder().Write("labels/a.json", Label(grader: "no-such-grader"));

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("a.json").And.Message
                .Contains("no-such-grader"));
    }

    [Test]
    public void ALabelStillInTheOldShape_IsRefused_NotQuietlyReadAsUnlabelled()
    {
        using var folder = Folder().Write("labels/a.json", Label(label: "{\"meal\": true, \"note\": \"old\"}"))
            .Write("labels/b.json",
                SampleMode.Fill(
                    """{"grader": "reads-keys", "scenario": "sample-dev", "mode": "<ModeBeta>", "run": "r", "repeat": 1, "turn": 1, "answer": "Bobotie", "label": {"pass": true, "note": ""}}"""));

        var error = Assert.Throws<InvalidDataException>(() => DatasetLoader.Load(folder.Root, Graders))!;

        Assert.That(error.Message, Does.Contain("a.json").Or.Contain("b.json"),
            "the 6a.2 shape (one answer, no turns) is refused too");
    }

    [Test]
    public void ALabelWithNoTurns_HasNoAnswerToLabel_AndIsRefused()
    {
        using var folder = Folder().Write("labels/a.json", Label(turn: 0));

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("a.json").And.Message.Contains("turn 0"));
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

        Assert.That(() => DatasetLoader.Load(folder.Root, Graders),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("a.json").And.Message.Contains("turn 3"));
    }

    [Test]
    public void AFileThatIsNotALabel_IsRefusedWithItsName()
    {
        using var folder = Folder().Write("labels/a.json", Label(extra: ", \"colour\": \"red\""))
            .Write("labels/b.json", "not json");

        var error = Assert.Throws<InvalidDataException>(() => DatasetLoader.Load(folder.Root, Graders))!;

        Assert.That(error.Message, Does.Contain("a.json").Or.Contain("b.json"));
    }

    [Test]
    public void LabellingChangesNoDatasetHash()
    {
        using var folder = Folder();
        string before = DatasetLoader.Load(folder.Root, Graders).Manifest.ContentHash;

        folder.Write("labels/a.json", Label());

        Assert.That(DatasetLoader.Load(folder.Root, Graders).Manifest.ContentHash, Is.EqualTo(before),
            "the labels are not scenarios: no version to bump");
    }

    /// <summary>A grader that is named only for the keys it reads; it never grades.</summary>
    private sealed class ReadsKeys(params string[] keys) : ICheck
    {
        public string Name => "reads-keys";
        public int Version => 1;
        public CheckKind Kind => CheckKind.FixedRule;
        public bool Required => true;
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>(keys);

        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    // 6a.5a: the API writes a label's text to the runner's stdin, so one label is read from its text with the same checks as from a file, named by the caller.
    [Test]
    public void OneLabel_IsReadFromItsText_WithTheSameChecks()
    {
        using var folder = Folder();
        var scenarios = DatasetLoader.Load(folder.Root, Graders).Scenarios;
        var graders = Graders.Select(g => g.Name).ToHashSet();

        var label = LabelLoader.Parse("line 1", Label(), scenarios, graders);

        Assert.That((label.File, label.Scenario, label.Repeat, label.Answer, label.Pass),
            Is.EqualTo(("line 1", "sample-dev", 2, "Bobotie vir vier", true)));
        Assert.That(() => LabelLoader.Parse("line 2", Label(scenario: "nope"), scenarios, graders),
            Throws.InstanceOf<InvalidDataException>().With.Message.Contains("line 2").And.Message.Contains("nope"));
    }
}
