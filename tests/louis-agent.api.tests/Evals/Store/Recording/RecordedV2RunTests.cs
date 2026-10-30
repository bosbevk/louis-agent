using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Recording;
using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Api.Tests.Evals.Store.Recording;

/// <summary>
/// The API records a run written in protocol version 2: the runner's own golden of the stub's run, and the contract's
/// golden of one conversation. Every event lands in the store as its document, every field kept, and only a line for
/// people goes to the run's log.
/// </summary>
public class RecordedV2RunTests
{
    /// <summary>The lines of a fixture linked into this project.</summary>
    private static string[] Fixture(string name) =>
        File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    /// <summary>Records the lines as one run that ends well, and hands back the store it was written to.</summary>
    private static async Task<(InMemoryEvalStore Store, string Run)> RecordAsync(string[] lines)
    {
        var store = new InMemoryEvalStore();
        var run = new RunningEval("run123456789", "stub", 1);
        var recording = new EvalRecorder(store).RecordAsync(run);
        foreach (string line in lines) run.Add(line);
        run.Complete(0);
        await recording;
        return (store, run.Id);
    }

    /// <summary>
    /// The stub's run: its eval, two modes with their rates, four conversations with their grades, six turns, the two
    /// summaries, and an empty log; the run says which protocol version it was recorded from.
    /// </summary>
    [Test]
    public async Task TheStubsRun_IsRecorded_EventByEvent()
    {
        var (store, id) = await RecordAsync(Fixture("stub-run.txt"));

        var run = (await store.GetRunAsync(id))!;
        Assert.That((run.Eval, run.Graders?.Count, run.Modes?.Count), Is.EqualTo(("stub", (int?)2, (int?)2)));
        Assert.That(run.Modes!.All(mode => mode.Rates is not null), Is.True, "each mode's rates from its mode-done");
        Assert.That(run.Slices, Has.Count.EqualTo(2), "a summary per mode");
        Assert.That(run.Log, Is.Empty, "every line was an event");
        Assert.That(run.ProtocolVersion, Is.EqualTo(2));

        var conversations = await store.ConversationsAsync(id);
        Assert.That(conversations, Has.Count.EqualTo(4));
        Assert.That(conversations.All(c => c.Grades is { Count: > 0 } && c.Outcome is not null), Is.True);
        int turns = 0;
        foreach (var conversation in conversations) turns += (await store.TurnsAsync(id, conversation.Key)).Count;
        Assert.That(turns, Is.EqualTo(6));
    }

    /// <summary>
    /// The contract's golden, field by field: the first line's split, commit, models and the human review's question,
    /// the turn's calls, the grades with their judge, and what the conversation measured.
    /// </summary>
    [Test]
    public async Task TheContractsGolden_IsRecorded_WithEveryField()
    {
        var (store, id) = await RecordAsync(Fixture("eval-protocol.jsonl"));

        var run = (await store.GetRunAsync(id))!;
        Assert.That((run.Eval, run.EvalVersion, run.Commit, run.Split),
            Is.EqualTo(("sample-eval", (int?)9, "5857373b", (RunSplit?)RunSplit.HeldOut)));
        Assert.That(run.Models, Has.Count.EqualTo(2));
        Assert.That(run.Graders!.Single(g => g.Kind == CheckKind.Human).Question,
            Is.EqualTo("Is this a meal for the people asked for?"));
        Assert.That(run.Slices!.Single().Graders.Single().ScoreMean, Is.EqualTo(0.5));
        Assert.That(run.Log, Is.Empty);

        var conversation = (await store.ConversationsAsync(id)).Single();
        Assert.That(conversation.Outcome, Is.EqualTo(TrialOutcome.Pass));
        Assert.That(conversation.Grades!.Select(g => (g.Grader, g.Outcome, g.JudgeModel)),
            Is.EqualTo(new[]
            {
                ("routed-to", GradeOutcome.Pass, (string?)null),
                ("meal-for-people", GradeOutcome.Unknown, "qwen3:4b"),
            }));
        Assert.That(conversation.Metrics, Is.Not.Null, "what the conversation measured");

        var turn = (await store.TurnsAsync(id, conversation.Key)).Single();
        Assert.That((turn.AgentCalls, turn.SpecialistCalls, turn.Hosts.Single()), Is.EqualTo((1, 2, "example.org")));
    }

    /// <summary>
    /// A line for people goes to the run's log, and so does an old <c>@</c> line, which is no event now; a first line
    /// of a protocol version this API doesn't read is logged and the run keeps no eval from it.
    /// </summary>
    [Test]
    public async Task LinesThatAreNoEvent_GoToTheLog_AndAnotherVersionsFirstLineIsNotRead()
    {
        string[] golden = Fixture("eval-protocol.jsonl");
        string thirdVersion = golden[0].Replace("\"version\":2", "\"version\":3");
        string v1 = """@{"event": "protocol", "t": 1.0, "version": 1}""";

        var (store, id) = await RecordAsync(["Running sample-eval", v1, thirdVersion]);

        var run = (await store.GetRunAsync(id))!;
        Assert.That(run.Log!.Take(2), Is.EqualTo(new[] { "Running sample-eval", v1 }));
        Assert.That(run.Log, Has.Count.EqualTo(3), "and a line saying version 3 isn't read");
        Assert.That(run.Eval, Is.Null);
    }
}
