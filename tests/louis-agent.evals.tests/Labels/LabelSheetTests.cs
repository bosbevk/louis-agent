using System.Text.Json;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Labels;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Tests.Labels;

// F18 step 6.3: `--label-sheet` turns a stored run's protocol export (the lines `--rescore` reads) into label files for a person to fill in: one per conversation, the answer
// in it, the label null. Only a scenario that sets what the grader reads can be labelled for it (a meal for four needs the four), and the sheet is a spread: one conversation of
// each scenario first, then a second of each, up to the count, so ten labels are not ten repeats of one dish. Written first: LabelSheet did not exist.
public class LabelSheetTests
{
    private static string At(object e) => "@" + JsonSerializer.Serialize(e);

    /// <summary>The stored lines of a run: a conversation per (task, mode, repeat), each with one turn that answered "answer of {task}/{mode}/{repeat}".</summary>
    private static string[] Stored(IEnumerable<string> tasks, string[] modes, int repeats)
    {
        var lines = new List<string> { At(new { @event = "protocol", version = 1, eval = "sample", eval_version = 1, graders = Array.Empty<object>(), models = Array.Empty<object>() }) };
        foreach (string mode in modes)
            for (int repeat = 1; repeat <= repeats; repeat++)
            {
                lines.Add(At(new { @event = "mode", mode, repeat, toolsets = new[] { "web" }, role = "r", llm = (string?)null, conversations = tasks.Count() }));
                foreach (string task in tasks)
                {
                    string answer = $"answer of {task}/{mode}/{repeat}";
                    lines.Add(At(new { @event = "question", mode, repeat, task, turn = 1, session = "sess_1", text = "q" }));
                    lines.Add(At(new { @event = "sse", mode, repeat, task, turn = 1, type = "content_block_delta", data = new { type = "content_block_delta", index = 0, delta = new { type = "text_delta", text = answer } } }));
                    lines.Add(At(new { @event = "turn", mode, repeat, task, turn = 1, session = "sess_1", question = "q", answer, role = (string?)null, seconds = 1.0 }));
                    lines.Add(At(new { @event = "conversation", mode, repeat, task, role = (string?)null, scores = new { }, errors = 0, seconds = 1.0 }));
                }
                lines.Add(At(new { @event = "mode_done", mode, repeat }));
            }
        return [.. lines];
    }

    private static EvalScenario Scenario(string id, string expected) =>
        new(id, new EvalInput(null, "Africa/Johannesburg", ["q"]), JsonDocument.Parse(expected).RootElement.Clone(), [], DatasetSplit.Dev);

    private static readonly IReadOnlyList<EvalScenario> Scenarios =
    [
        Scenario("cook-af", """{"serves":4}"""), Scenario("cook-en", """{"serves":2}"""), Scenario("cook-shops", """{"serves":4}"""), Scenario("control-web", """{"routes_to":"puls"}"""),
    ];

    private static string TempFolder() => Path.Combine(Path.GetTempPath(), "labels-" + Guid.NewGuid().ToString("N"));

    private static List<LabelFile> Written(string folder) =>
        Directory.GetFiles(folder).Order().Select(f => JsonSerializer.Deserialize<LabelFile>(File.ReadAllText(f), LabelJson.Options)!).ToList();

    [Test]
    public void OneFilePerConversation_WithTheAnswerInIt_AndNoLabelYet()
    {
        string folder = TempFolder();
        try
        {
            var names = LabelSheet.Write(Scenarios, "meal-for-people", ["serves"], "8f7802b9a72e", Stored(["cook-af"], ["puls-cook"], 1), folder, count: 10);

            Assert.That(names, Is.EqualTo(new[] { "meal-for-people.cook-af.puls-cook.8f7802b9a72e.1.json" }));
            var file = Written(folder).Single();
            Assert.That((file.Scenario, file.Mode, file.Run, file.Repeat, file.Turn, file.Answer), Is.EqualTo(("cook-af", "puls-cook", "8f7802b9a72e", 1, 1, "answer of cook-af/puls-cook/1")));
            Assert.That(file.Turns, Is.EqualTo(new[] { new LabelTurn("q", "answer of cook-af/puls-cook/1") }), "6a.4f: every turn, question and answer");
            Assert.That(file.Label, Is.Null, "the person's part");
            Assert.That(file.Grader, Is.EqualTo("meal-for-people"), "the grader it is a label for");
        }
        finally { Directory.Delete(folder, true); }
    }

    [Test]
    public void OnlyScenariosThatSetWhatTheGraderReads_AreInTheSheet()
    {
        string folder = TempFolder();
        try
        {
            LabelSheet.Write(Scenarios, "meal-for-people", ["serves"], "run", Stored(["cook-af", "control-web"], ["puls-cook"], 1), folder, count: 10);

            Assert.That(Written(folder).Select(f => f.Scenario), Is.EqualTo(new[] { "cook-af" }), "the control question sets no serves: no meal to label");
        }
        finally { Directory.Delete(folder, true); }
    }

    [Test]
    public void TheSheetIsASpread_EachScenarioOnceBeforeAnyTwice_AndNoMoreThanTheCount()
    {
        string folder = TempFolder();
        try
        {
            var names = LabelSheet.Write(Scenarios, "meal-for-people", ["serves"], "run", Stored(["cook-af", "cook-en", "cook-shops"], ["puls-tools", "puls-cook"], 3), folder, count: 4);

            var files = Written(folder);
            Assert.That(names, Has.Count.EqualTo(4));
            Assert.That(files.Select(f => f.Scenario).Distinct(), Is.EquivalentTo(new[] { "cook-af", "cook-en", "cook-shops" }), "all three before a second of any");
            Assert.That(files.GroupBy(f => f.Scenario).Max(g => g.Count()), Is.EqualTo(2));
            Assert.That(files.Select(f => (f.Scenario, f.Mode, f.Repeat)).Distinct().Count(), Is.EqualTo(4), "no conversation twice");
        }
        finally { Directory.Delete(folder, true); }
    }

    [Test]
    public void TheSameSheetTwice_IsTheSameFiles()
    {
        string one = TempFolder(), two = TempFolder();
        try
        {
            var stored = Stored(["cook-af", "cook-en", "cook-shops"], ["puls-tools", "puls-cook"], 3);

            var first = LabelSheet.Write(Scenarios, "meal-for-people", ["serves"], "run", stored, one, count: 5);
            var second = LabelSheet.Write(Scenarios, "meal-for-people", ["serves"], "run", stored, two, count: 5);

            Assert.That(second, Is.EqualTo(first), "a person who asks twice labels the same answers");
        }
        finally { Directory.Delete(one, true); Directory.Delete(two, true); }
    }

    // A second sheet for the same run (fresh answers to check a tuned judge on) goes into the folder the first one was labelled in: it must never write over a person's labels,
    // and it takes the next conversations instead, so the count is of new files.
    [Test]
    public void AFileThatExists_IsNeverOverwritten_AndTheNextConversationIsTakenInstead()
    {
        string folder = TempFolder();
        try
        {
            var stored = Stored(["cook-af", "cook-en"], ["puls-cook"], 3);
            LabelSheet.Write(Scenarios, "meal-for-people", ["serves"], "run", stored, folder, count: 2);
            string labelled = Path.Combine(folder, "meal-for-people.cook-af.puls-cook.run.1.json");
            File.WriteAllText(labelled, File.ReadAllText(labelled).Replace("\"label\": null", "\"label\": { \"meal\": true, \"note\": \"mine\" }"));

            var second = LabelSheet.Write(Scenarios, "meal-for-people", ["serves"], "run", stored, folder, count: 2);

            Assert.That(File.ReadAllText(labelled), Does.Contain("mine"), "the person's label is still there");
            Assert.That(second, Is.EqualTo(new[] { "meal-for-people.cook-af.puls-cook.run.2.json", "meal-for-people.cook-en.puls-cook.run.2.json" }), "two new files, from the next repeat");
            Assert.That(Directory.GetFiles(folder), Has.Length.EqualTo(4));
        }
        finally { Directory.Delete(folder, true); }
    }

    // F18 step 6a.0b (the review): the grader is in the file's name, so two graders labelling the same answer have two files. Without it the second sheet would find the first grader's file
    // already there and, because a sheet never overwrites, silently write nothing for the second grader.
    [Test]
    public void TwoGradersLabellingTheSameAnswers_HaveTwoFilesEach_NoneSkipped()
    {
        string folder = TempFolder();
        try
        {
            var stored = Stored(["cook-af"], ["puls-cook"], 1);

            var first = LabelSheet.Write(Scenarios, "meal-for-people", ["serves"], "run", stored, folder, count: 10);
            var second = LabelSheet.Write(Scenarios, "another-judge", ["serves"], "run", stored, folder, count: 10);

            Assert.That(first, Is.EqualTo(new[] { "meal-for-people.cook-af.puls-cook.run.1.json" }));
            Assert.That(second, Is.EqualTo(new[] { "another-judge.cook-af.puls-cook.run.1.json" }), "not skipped for the first grader's file");
            Assert.That(Written(folder).Select(f => f.Grader), Is.EquivalentTo(new[] { "meal-for-people", "another-judge" }));
        }
        finally { Directory.Delete(folder, true); }
    }

    [Test]
    public void ARunThatIsNotAnEvalsRun_IsRefused()
    {
        string folder = TempFolder();

        Assert.That(() => LabelSheet.Write(Scenarios, "meal-for-people", ["serves"], "run", [At(new { @event = "protocol", version = 1 })], folder, count: 10),
            Throws.TypeOf<InvalidDataException>());
    }
}
