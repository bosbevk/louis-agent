using LouisAgent.Composition.Agents;
using LouisAgent.Core.Llm.Replay;
using LouisAgent.Core.Llm.UsageMapping;
using LouisAgent.Core.Pricing.Prices;
using LouisAgent.Core.Sessions.Replay.Store;
using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Transcripts;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Turns;
using LouisAgent.Evals.Runs;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Evals.Tests.Runs.Regrades;

/// <summary>
/// F126.4: a re-grade grades a run's stored answers again against the eval file as it is now, with no agent asked.
/// Facts are told again in code; a mark is kept wherever its turn and its criterion's words are the same; the judge is
/// asked only for a criterion that is new or reworded, and its answers are recorded beside the live run's own, so a
/// replay of that live run finds them.
/// </summary>
public class RegraderTests
{
    private const string LiveId = "20261123-040000-guide";
    private const string Model = "judge-model";
    private const string Plainly = "Says it plainly";

    private static readonly DateTimeOffset Started = new(2026, 11, 23, 16, 0, 0, TimeSpan.Zero);
    private static readonly Flip Flipped = new(Started.AddHours(-1), "It does.");

    private static readonly TurnTranscript Said =
        new("What is louis-agent?", "A framework.", [], TimeSpan.FromSeconds(4));

    private static readonly PriceTable Prices = new()
    {
        AsOf = "2026-11-23",
        Currency = "USD",
        Models = new Dictionary<string, ModelPrice>
        {
            [Model] = new()
            {
                Input = 1m,
                Output = 5m,
                CacheRead = 0.1m,
                CacheWrite5m = 1.25m,
                CacheWrite1h = 2m
            }
        }
    };

    private string _folder = "";
    private int _judgeCalls;

    /// <summary>A recordings folder of this test's own, and a judge that hasn't been called.</summary>
    [SetUp]
    public void MakeFolder()
    {
        _folder = Directory.CreateTempSubdirectory("regrade-").FullName;
        _judgeCalls = 0;
    }

    /// <summary>The folder goes when the test is done.</summary>
    [TearDown]
    public void RemoveFolder() => Directory.Delete(_folder, recursive: true);

    /// <summary>
    /// Against an unchanged file nothing is asked of the judge: every mark is kept with its flip, every fact is told
    /// again in code, and the run passes as it did.
    /// </summary>
    [Test]
    public async Task AnUnchangedFile_CallsTheJudgeNever_KeepsEveryMark_AndTellsEveryFactAgain()
    {
        var graded = Live(repeats: 2);

        var run = await Regrade(graded, File([Plainly]));

        var marks = run.Conversations[0].Repeats.SelectMany(repeat => repeat.Turns).ToList();
        Assert.That(_judgeCalls, Is.Zero);
        Assert.That(marks.Select(turn => turn.Criteria[0]), Has.All.EqualTo(graded.Conversations[0].Repeats[0]
            .Turns[0].Criteria[0]));
        Assert.That(marks.Select(turn => turn.Facts[0].Reason), Has.None.EqualTo("Told long ago."));
        Assert.That((run.Passed, graded.Passed, run.Status), Is.EqualTo((1, 1, RunStatus.Finished)));
    }

    /// <summary>A new criterion is judged once a repeat, a reworded one too, and one taken out is gone.</summary>
    [Test]
    public async Task ANewOrRewordedCriterion_IsJudgedOnceARepeat_AndOneTakenOutIsGone()
    {
        var graded = Live(repeats: 2);

        var run = await Regrade(graded, File(["Says it plainly, in one line", "Names the framework"]));

        var criteria = run.Conversations[0].Repeats[0].Turns[0].Criteria.Select(mark => mark.Text);
        Assert.That(_judgeCalls, Is.EqualTo(4), "two criteria, two repeats");
        Assert.That(criteria, Is.EqualTo(new[] { "Says it plainly, in one line", "Names the framework" }));
    }

    /// <summary>A conversation the run lacks, or whose asks changed, is left out: no answer to it is stored.</summary>
    [Test]
    public async Task AConversationTheRunLacks_OrWhoseAsksChanged_IsLeftOut()
    {
        var graded = Live(repeats: 1);
        var file = new EvalFile(new AgentAddress("louis-agent-guide", null),
        [
            new Conversation("guide-what-is", [new Turn("What is louis-agent, then?") { Criteria = [Plainly] }]),
            new Conversation("guide-pricing", [new Turn("What does it cost?")])
        ]);

        var run = await Regrade(graded, file);

        Assert.That(run.Conversations, Is.Empty);
        Assert.That(run.Status, Is.EqualTo(RunStatus.Finished));
    }

    /// <summary>
    /// An ask that writes {trial} was sent to each repeat with its own id, so the stored answers are kept against the
    /// file's ask filled for each repeat, and nothing is asked of the judge (FX257).
    /// </summary>
    [Test]
    public async Task AnAskThatWritesTheTrial_IsKept_ForEachRepeatsOwnId()
    {
        const string Ask = "Run id: eval-refund-{trial}.";
        var asked = Enumerable.Range(1, 2).Select(number =>
            new TurnRun(Said with { Ask = $"Run id: eval-refund-{LiveId}-{number}." },
                [new Mark("no-error", true, "Told long ago.")], [new Mark(Plainly, true, "It says so.")], 0.001m,
                "USD")).ToList();
        var graded = Live(repeats: 2) with
        {
            Conversations =
            [new ConversationRun("guide-what-is", 2, [new RepeatRun(1, [asked[0]]), new RepeatRun(2, [asked[1]])])]
        };
        var file = new EvalFile(new AgentAddress("louis-agent-guide", null),
            [new Conversation("guide-what-is", [new Turn(Ask) { Criteria = [Plainly] }])]);

        var run = await Regrade(graded, file);

        Assert.That(run.Conversations.Select(conversation => conversation.Id), Is.EqualTo(new[] { "guide-what-is" }));
        Assert.That(_judgeCalls, Is.Zero);
    }

    /// <summary>
    /// The judge's new answers are recorded under the live run's play, beside its old ones, and a judge replaying
    /// that play then marks both with no model called.
    /// </summary>
    [Test]
    public async Task TheJudgesNewAnswers_AreRecordedBesideTheLiveRunsOwn_AndAReplayFindsBoth()
    {
        var graded = Live(repeats: 1);
        var live = new RunTrial(LiveId, "guide-what-is", 1);
        using (JudgeReplay.Begin(live))
            await Judge().MarkAsync(Plainly, [Said]);
        int beforeRegrade = _judgeCalls;

        await Regrade(graded, File([Plainly, "Names the framework"]));
        var recorded = await new ReplayStore(_folder).AsksAsync($"{LiveId}/guide-what-is.1", ReplayKind.Model);
        int afterRegrade = _judgeCalls;
        using (JudgeReplay.Begin(new RunTrial("20261123-170000-guide", "guide-what-is", 1, LiveId)))
        {
            await Judge().MarkAsync(Plainly, [Said]);
            await Judge().MarkAsync("Names the framework", [Said]);
        }

        Assert.That((beforeRegrade, afterRegrade), Is.EqualTo((1, 2)), "only the new criterion was asked");
        Assert.That(recorded, Has.Count.EqualTo(2), "the old answer and the new one");
        Assert.That(_judgeCalls, Is.EqualTo(afterRegrade), "the replay called no model");
    }

    /// <summary>
    /// A re-graded turn's agent cost is a known 0, since no agent was asked; its judge cost is what its new calls
    /// cost, and a kept mark costs a known 0.
    /// </summary>
    [Test]
    public async Task ARegradedTurn_CostsTheAgentNothing_AndTheJudgeWhatItsNewCallsCost()
    {
        var graded = Live(repeats: 1);

        var kept = (await Regrade(graded, File([Plainly]))).Conversations[0].Repeats[0].Turns[0];
        var judged = (await Regrade(graded, File([Plainly, "Names the framework"]))).Conversations[0].Repeats[0]
            .Turns[0];

        Assert.That((kept.Cost, kept.JudgeCost), Is.EqualTo(((decimal?)0m, (decimal?)0m)));
        Assert.That(judged.Cost, Is.EqualTo(0m));
        Assert.That(judged.JudgeCost, Is.GreaterThan(0m));
    }

    /// <summary>A judge that can't mark ends the re-grade as could not finish, keeping what was graded.</summary>
    [Test]
    public async Task AJudgeThatFails_EndsItAsCouldNotFinish_KeepingWhatWasGraded()
    {
        var graded = Live(repeats: 1) with
        {
            Conversations = [.. Live(repeats: 1).Conversations, Conversation("guide-again", 1)]
        };
        var file = new EvalFile(new AgentAddress("louis-agent-guide", null),
        [
            new Conversation("guide-what-is", [
                new Turn(Said.Ask)
                {
                    Facts = [Fact.Parse("no-error")],
                    Criteria = [Plainly]
                }
            ]),
            new Conversation("guide-again", [new Turn(Said.Ask) { Criteria = ["Something new"] }])
        ]);

        var run = await Regrade(graded, file, failing: true);

        Assert.That(run.Status, Is.EqualTo(RunStatus.CouldNotFinish));
        Assert.That(run.Conversations.Select(c => c.Id), Is.EqualTo(new[] { "guide-what-is" }));
    }

    /// <summary>The run re-graded against this file, kept nowhere, with a judge that fails if asked to.</summary>
    private async Task<EvalRun> Regrade(EvalRun graded, EvalFile file, bool failing = false)
    {
        var started = new EvalRun("20261123-160000-guide", "guide", "louis-agent-guide", RunKind.Regrade,
            graded.Repeats, Started, [])
        {
            RegradeOf = graded.Id,
            ReplayOf = LiveId
        };
        var regrader = new Regrader(new TurnGrader(Judge(failing)), new FakeTimeProvider(Started.AddMinutes(1)));
        return await regrader.RegradeAsync(started, graded, file, _ => Task.CompletedTask);
    }

    /// <summary>
    /// The judge as a host composes it: its model, recorded and replayed through the recordings folder, priced on a
    /// ledger. The model counts its calls and marks every criterion met, or answers nothing a judge can read.
    /// </summary>
    private Judge Judge(bool failing = false)
    {
        var model = new FakeChatClient(Enumerable.Repeat<Func<IList<ChatMessage>, ChatResponse>>(_ =>
        {
            _judgeCalls++;
            return failing ? new ChatResponse(new ChatMessage(ChatRole.Assistant, "")) : Met();
        }, 20).ToArray());
        var replaying = new JudgeReplay(model, new ReplayStore(_folder));
        return new Judge(new UsageRecordingChatClient(replaying, new PricingUsageSink(new ListUsageSink(), Prices),
            Model));
    }

    /// <summary>The judge's answer met, with the tokens it used.</summary>
    private static ChatResponse Met() =>
        new(new ChatMessage(ChatRole.Assistant, """{"reason":"Read the answer.","met":true}"""))
        {
            ModelId = Model,
            Usage = new UsageDetails
            {
                InputTokenCount = 1000,
                OutputTokenCount = 100
            }
        };

    /// <summary>The eval file now: the one conversation with one fact and these criteria.</summary>
    private static EvalFile File(IReadOnlyList<string> criteria) =>
        new(new AgentAddress("louis-agent-guide", null),
        [
            new Conversation("guide-what-is", [
                new Turn(Said.Ask)
                {
                    Facts = [Fact.Parse("no-error")],
                    Criteria = criteria
                }
            ])
        ]);

    /// <summary>
    /// A finished live run of the one conversation: its fact told long ago, and its criterion missed by the judge and
    /// flipped to met by a person, so it passed.
    /// </summary>
    private static EvalRun Live(int repeats) =>
        new(LiveId, "guide", "louis-agent-guide", RunKind.Live, repeats, Started.AddHours(-2),
            [Conversation("guide-what-is", repeats)])
        {
            Status = RunStatus.Finished,
            EndedAt = Started.AddHours(-2).AddMinutes(1)
        };

    /// <summary>A conversation of this many repeats of the one turn.</summary>
    private static ConversationRun Conversation(string id, int repeats)
    {
        var criterion = new Mark(Plainly, false, "It doesn't say so.") { Flip = Flipped };
        var turn = new TurnRun(Said, [new Mark("no-error", true, "Told long ago.")], [criterion], 0.001m, "USD");
        return new ConversationRun(id, repeats,
            [.. Enumerable.Range(1, repeats).Select(number => new RepeatRun(number, [turn]))]);
    }
}
