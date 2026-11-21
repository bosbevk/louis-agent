using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Labels;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;

namespace LouisAgent.Evals.Tests.Labels;

/// <summary>
/// <c>--label-sheet</c> turns a stored run's protocol export (the lines <c>--rescore</c> reads) into label files for a
/// person to fill in (F18 step 6.3): one per conversation, the answer in it, the label empty. Only a scenario that
/// sets what the grader reads can be labelled for it (a meal for four needs the four), and the sheet is a spread: one
/// conversation of each scenario first, then a second of each, up to the count.
/// </summary>
public class LabelSheetTests
{
    /// <summary>The time every line carries.</summary>
    private static readonly DateTimeOffset At = new(2026, 10, 28, 8, 0, 0, TimeSpan.Zero);

    /// <summary>The scenarios the sheets are made for: three ask for a number of people, the control doesn't.</summary>
    private static readonly IReadOnlyList<EvalScenario> Scenarios =
    [
        Scenario(SampleScenario.Alpha, """{"serves":4}"""), Scenario(SampleScenario.Beta, """{"serves":2}"""),
        Scenario("cook-shops", """{"serves":4}"""), Scenario("control-web", """{"routes_to":"puls"}"""),
    ];

    /// <summary>
    /// A stored run: a conversation per task, mode and repeat, each with one turn that answered
    /// "answer of {task}/{mode}/{repeat}".
    /// </summary>
    private static string[] Stored(IEnumerable<string> tasks, string[] modes, int repeats)
    {
        var events = new List<EvalEvent>
        {
            new ProtocolEvent(At, ProtocolEvent.CurrentVersion, "sample", 1, "abc123", RunSplit.Dev, null, null, [],
                []),
        };
        foreach (string mode in modes)
            for (int repeat = 1; repeat <= repeats; repeat++)
            {
                events.Add(new ModeEvent(At, mode, repeat, [SampleToolset.Alpha], "r", null, tasks.Count()));
                foreach (string task in tasks) events.AddRange(Conversation(mode, repeat, task));
                events.Add(new ModeDoneEvent(At, mode, repeat, null));
            }

        return [.. events.Select(EvalProtocolJson.Line)];
    }

    /// <summary>One conversation's lines: the question, the answer streamed, the turn and the result.</summary>
    private static EvalEvent[] Conversation(string mode, int repeat, string task)
    {
        string answer = $"answer of {task}/{mode}/{repeat}";
        var delta = JsonDocument.Parse(
                $$$"""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"{{{answer}}}"}}""")
            .RootElement.Clone();
        return
        [
            new QuestionEvent(At, mode, repeat, task, 1, "sess_1", "q"),
            new SseEvent(At, mode, repeat, task, 1, "content_block_delta", delta),
            new TurnEvent(At, mode, repeat, task, 1, "sess_1", "q", answer, null, null, 0, 0, [], 1.0, null, null),
            new ConversationEvent(At, mode, repeat, task, null, 0, 1.0, [], TrialOutcome.Pass,
                new Metrics(0, 0, [], 1, null, null, 1.0, null)),
        ];
    }

    /// <summary>A scenario with what it expects.</summary>
    private static EvalScenario Scenario(string id, string expected) =>
        new(id, new EvalInput(null, "Africa/Johannesburg", ["q"]), JsonDocument.Parse(expected).RootElement.Clone(), [],
            DatasetSplit.Dev);

    /// <summary>A new folder under the temp folder, for one test's sheet.</summary>
    private static string TempFolder() => Path.Combine(Path.GetTempPath(), "labels-" + Guid.NewGuid().ToString("N"));

    /// <summary>The label files in a folder, read back, in name order.</summary>
    private static List<LabelFile> Written(string folder) =>
        Directory.GetFiles(folder).Order()
            .Select(f => JsonSerializer.Deserialize<LabelFile>(File.ReadAllText(f), LabelJson.Options)!).ToList();

    /// <summary>A conversation's file holds every turn and the answer to label, and no label yet.</summary>
    [Test]
    public void OneFilePerConversation_WithTheAnswerInIt_AndNoLabelYet()
    {
        string folder = TempFolder();
        try
        {
            var names = LabelSheet.Write(Scenarios, "meal-for-people", ["serves"], "8f7802b9a72e",
                Stored([SampleScenario.Alpha], [SampleMode.Beta], 1), folder, count: 10);

            Assert.That(names,
                Is.EqualTo(new[]
                {
                    SampleScenario.Fill($"meal-for-people.<ScenarioAlpha>.{SampleMode.Beta}.8f7802b9a72e.1.json")
                }));
            var file = Written(folder).Single();
            Assert.That((file.Scenario, file.Mode, file.Run, file.Repeat, file.Turn, file.Answer),
                Is.EqualTo((SampleScenario.Alpha, SampleMode.Beta, "8f7802b9a72e", 1, 1,
                    SampleScenario.Fill($"answer of <ScenarioAlpha>/{SampleMode.Beta}/1"))));
            Assert.That(file.Turns,
                Is.EqualTo(new[]
                {
                    new LabelTurn("q", SampleScenario.Fill($"answer of <ScenarioAlpha>/{SampleMode.Beta}/1"))
                }),
                "6a.4f: every turn, question and answer");
            Assert.That(file.Label, Is.Null, "the person's part");
            Assert.That(file.Grader, Is.EqualTo("meal-for-people"), "the grader it is a label for");
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    /// <summary>A scenario that doesn't set what the grader reads has nothing to label.</summary>
    [Test]
    public void OnlyScenariosThatSetWhatTheGraderReads_AreInTheSheet()
    {
        string folder = TempFolder();
        try
        {
            LabelSheet.Write(Scenarios, "meal-for-people", ["serves"], "run",
                Stored([SampleScenario.Alpha, "control-web"], [SampleMode.Beta], 1), folder, count: 10);

            Assert.That(Written(folder).Select(f => f.Scenario), Is.EqualTo(new[] { SampleScenario.Alpha }),
                "the control question sets no serves: no meal to label");
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    /// <summary>Every scenario once before any twice, and never more than the count.</summary>
    [Test]
    public void TheSheetIsASpread_EachScenarioOnceBeforeAnyTwice_AndNoMoreThanTheCount()
    {
        string folder = TempFolder();
        try
        {
            var names = LabelSheet.Write(Scenarios, "meal-for-people", ["serves"], "run",
                Stored([SampleScenario.Alpha, SampleScenario.Beta, "cook-shops"], [SampleMode.Gamma, SampleMode.Beta],
                    3), folder, count: 4);

            var files = Written(folder);
            Assert.That(names, Has.Count.EqualTo(4));
            Assert.That(files.Select(f => f.Scenario).Distinct(),
                Is.EquivalentTo(new[] { SampleScenario.Alpha, SampleScenario.Beta, "cook-shops" }),
                "all three before a second of any");
            Assert.That(files.GroupBy(f => f.Scenario).Max(g => g.Count()), Is.EqualTo(2));
            Assert.That(files.Select(f => (f.Scenario, f.Mode, f.Repeat)).Distinct().Count(), Is.EqualTo(4),
                "no conversation twice");
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    /// <summary>Asking twice gives the same sheet.</summary>
    [Test]
    public void TheSameSheetTwice_IsTheSameFiles()
    {
        string one = TempFolder(), two = TempFolder();
        try
        {
            var stored = Stored([SampleScenario.Alpha, SampleScenario.Beta, "cook-shops"],
                [SampleMode.Gamma, SampleMode.Beta], 3);

            var first = LabelSheet.Write(Scenarios, "meal-for-people", ["serves"], "run", stored, one, count: 5);
            var second = LabelSheet.Write(Scenarios, "meal-for-people", ["serves"], "run", stored, two, count: 5);

            Assert.That(second, Is.EqualTo(first), "a person who asks twice labels the same answers");
        }
        finally
        {
            Directory.Delete(one, true);
            Directory.Delete(two, true);
        }
    }

    /// <summary>
    /// A second sheet for the same run goes into the folder the first was labelled in: it never writes over a person's
    /// labels, and takes the next conversations instead, so the count is of new files.
    /// </summary>
    [Test]
    public void AFileThatExists_IsNeverOverwritten_AndTheNextConversationIsTakenInstead()
    {
        string folder = TempFolder();
        try
        {
            var stored = Stored([SampleScenario.Alpha, SampleScenario.Beta], [SampleMode.Beta], 3);
            LabelSheet.Write(Scenarios, "meal-for-people", ["serves"], "run", stored, folder, count: 2);
            string labelled = Path.Combine(folder,
                SampleScenario.Fill($"meal-for-people.<ScenarioAlpha>.{SampleMode.Beta}.run.1.json"));
            File.WriteAllText(labelled,
                File.ReadAllText(labelled)
                    .Replace("\"label\": null", "\"label\": { \"meal\": true, \"note\": \"mine\" }"));

            var second = LabelSheet.Write(Scenarios, "meal-for-people", ["serves"], "run", stored, folder, count: 2);

            Assert.That(File.ReadAllText(labelled), Does.Contain("mine"), "the person's label is still there");
            Assert.That(second,
                Is.EqualTo(new[]
                {
                    SampleScenario.Fill($"meal-for-people.<ScenarioAlpha>.{SampleMode.Beta}.run.2.json"),
                    SampleScenario.Fill($"meal-for-people.<ScenarioBeta>.{SampleMode.Beta}.run.2.json")
                }), "two new files, from the next repeat");
            Assert.That(Directory.GetFiles(folder), Has.Length.EqualTo(4));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    /// <summary>
    /// The grader is in the file's name, so two graders labelling the same answer get a file each (F18 step 6a.0b).
    /// Without it the second sheet would find the first grader's file and, since a sheet never overwrites, write
    /// nothing.
    /// </summary>
    [Test]
    public void TwoGradersLabellingTheSameAnswers_HaveTwoFilesEach_NoneSkipped()
    {
        string folder = TempFolder();
        try
        {
            var stored = Stored([SampleScenario.Alpha], [SampleMode.Beta], 1);

            var first = LabelSheet.Write(Scenarios, "meal-for-people", ["serves"], "run", stored, folder, count: 10);
            var second = LabelSheet.Write(Scenarios, "another-judge", ["serves"], "run", stored, folder, count: 10);

            Assert.That(first,
                Is.EqualTo(new[]
                {
                    SampleScenario.Fill($"meal-for-people.<ScenarioAlpha>.{SampleMode.Beta}.run.1.json")
                }));
            Assert.That(second,
                Is.EqualTo(new[]
                {
                    SampleScenario.Fill($"another-judge.<ScenarioAlpha>.{SampleMode.Beta}.run.1.json")
                }),
                "not skipped for the first grader's file");
            Assert.That(Written(folder).Select(f => f.Grader),
                Is.EquivalentTo(new[] { "meal-for-people", "another-judge" }));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    /// <summary>Lines with no first line naming an eval aren't an eval's run, and are refused.</summary>
    [Test]
    public void ARunThatIsNotAnEvalsRun_IsRefused()
    {
        string folder = TempFolder();
        string[] lines = [EvalProtocolJson.Line(new ModeEvent(At, SampleMode.Beta, 1, [], "r", null, 1))];

        Assert.That(() => LabelSheet.Write(Scenarios, "meal-for-people", ["serves"], "run", lines, folder, count: 10),
            Throws.TypeOf<InvalidDataException>());
    }
}
