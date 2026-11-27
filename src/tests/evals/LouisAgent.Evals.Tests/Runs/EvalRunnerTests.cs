using LouisAgent.Composition.Agents;
using LouisAgent.Core.Llm.Failures;
using LouisAgent.Core.Sessions.Replay.Store;
using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Transcripts;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Files.Pages;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Runs;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Time.Testing;
using static LouisAgent.Evals.Tests.Runs.FakeSessionsApi;

namespace LouisAgent.Evals.Tests.Runs;

/// <summary>
/// The run plays every conversation as many times as it asks, keeps itself after each one, and ends as finished,
/// or as could not finish when the harness fails: a judge that can't mark, or an API that refuses.
/// </summary>
public class EvalRunnerTests
{
    private static readonly DateTimeOffset Started = new(2026, 11, 23, 4, 30, 12, TimeSpan.Zero);
    private static readonly DateTimeOffset Ended = Started.AddMinutes(2);

    /// <summary>The guide's eval of two conversations, one turn each, checked by a fact alone.</summary>
    private static readonly EvalFile TwoConversations = new(new AgentAddress("louis-agent-guide", null),
    [
        new Conversation("guide-what-is", [new Turn("What is louis-agent?") { Facts = [Fact.Parse("no-error")] }]),
        new Conversation("guide-pricing", [new Turn("What does it cost?") { Facts = [Fact.Parse("no-error")] }])
    ]);

    /// <summary>With three repeats, each conversation is played three times, each in a session of its own.</summary>
    [Test]
    public async Task ThreeRepeats_PlayEachConversationThreeTimes_EachInItsOwnSession()
    {
        var api = new FakeSessionsApi(Enumerable.Repeat(Answered("Yes."), 6).ToArray());

        var run = await Runner(api).RunAsync(StartedRun(3), TwoConversations, Kept([]));

        Assert.That(run.Conversations.Select(c => (c.Id, c.Repeats.Count)),
            Is.EqualTo(new[] { ("guide-what-is", 3), ("guide-pricing", 3) }));
        Assert.That(api.Opened(), Has.Count.EqualTo(6));
        Assert.That(api.Calls().Count(call => call.StartsWith("DELETE", StringComparison.Ordinal)), Is.EqualTo(6));
    }

    /// <summary>With one repeat, each is played once.</summary>
    [Test]
    public async Task OneRepeat_PlaysEachConversationOnce()
    {
        var api = new FakeSessionsApi(Answered("Yes."), Answered("Yes."));

        var run = await Runner(api).RunAsync(StartedRun(1), TwoConversations, Kept([]));

        Assert.That(run.Conversations.Select(c => c.Repeats.Count), Is.EqualTo(new[] { 1, 1 }));
        Assert.That(run.Passed, Is.EqualTo(2));
    }

    /// <summary>The run is kept after every conversation, with those graded so far, and once more as it ends.</summary>
    [Test]
    public async Task TheRun_IsKeptAfterEveryConversation_AndAsItEnds()
    {
        var api = new FakeSessionsApi(Answered("Yes."), Answered("Yes."));
        var kept = new List<EvalRun>();

        await Runner(api).RunAsync(StartedRun(1), TwoConversations, Kept(kept));

        Assert.That(kept.Select(run => (run.Conversations.Count, run.Status)),
            Is.EqualTo(new[] { (1, RunStatus.Running), (2, RunStatus.Running), (2, RunStatus.Finished) }));
    }

    /// <summary>A run that played every conversation ends as finished, at the clock's time.</summary>
    [Test]
    public async Task TheRun_EndsAsFinished_WithItsEndTime()
    {
        var api = new FakeSessionsApi(Answered("Yes."), Answered("Yes."));

        var run = await Runner(api).RunAsync(StartedRun(1), TwoConversations, Kept([]));

        Assert.That((run.Status, run.EndedAt, run.StoppedBecause),
            Is.EqualTo((RunStatus.Finished, Ended, (string?)null)));
    }

    /// <summary>
    /// A judge that can't mark ends the run as could not finish, with the judge's reason, keeping what was graded
    /// before the stop.
    /// </summary>
    [Test]
    public async Task AJudgeThatCantMark_EndsTheRunAsCouldNotFinish_WithTheJudgesReason()
    {
        var file = TwoConversations with
        {
            Conversations =
            [
                TwoConversations.Conversations[0],
                new Conversation("guide-pricing", [new Turn("What does it cost?") { Criteria = ["Says it is free"] }])
            ]
        };
        var api = new FakeSessionsApi(Answered("Yes."), Answered("It is free."));
        var noAnswer = new FakeChatClient(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "")));
        var kept = new List<EvalRun>();

        var run = await Runner(api, noAnswer).RunAsync(StartedRun(1), file, Kept(kept));

        Assert.That((run.Status, run.EndedAt), Is.EqualTo((RunStatus.CouldNotFinish, (DateTimeOffset?)Ended)));
        Assert.That(run.StoppedBecause, Does.Contain("The judge gave no answer."));
        Assert.That(run.Conversations.Select(c => c.Id), Is.EqualTo(new[] { "guide-what-is" }));
        Assert.That(kept[^1], Is.EqualTo(run));
    }

    /// <summary>
    /// The agent's model at its account limit stops the run at once as could-not-finish, naming the limit, instead of
    /// failing every conversation after it (F8).
    /// </summary>
    [Test]
    public async Task TheAgentsAccountLimit_StopsTheRunAtOnce_NamingIt()
    {
        const string words = "This agent has reached its usage limit and can't answer until 2026-12-01 00:00 UTC.";
        var api = new FakeSessionsApi(AccountLimited(words), Answered("never asked"));
        var kept = new List<EvalRun>();

        var run = await Runner(api).RunAsync(StartedRun(1), TwoConversations, Kept(kept));

        Assert.Multiple(() =>
        {
            Assert.That(run.Status, Is.EqualTo(RunStatus.CouldNotFinish));
            Assert.That(run.StoppedBecause, Does.Contain("account").And.Contain("2026-12-01 00:00 UTC"));
            Assert.That(run.Conversations, Is.Empty, "the conversation it stopped in has no graded repeat");
            Assert.That(api.Asked(), Has.Count.EqualTo(1), "nothing more was asked");
        });
    }

    /// <summary>The judge's model at its account limit stops the run the same way, naming it.</summary>
    [Test]
    public async Task TheJudgesAccountLimit_StopsTheRun_NamingIt()
    {
        var file = TwoConversations with
        {
            Conversations =
            [
                new Conversation("guide-pricing", [new Turn("What does it cost?") { Criteria = ["Says it is free"] }])
            ]
        };
        var api = new FakeSessionsApi(Answered("It is free."));
        var limit = ModelFailure.AccountLimit("Your credit balance is too low.", null);
        var judgeAtItsLimit = new FakeChatClient(_ => throw new AccountLimitException(limit));

        var run = await Runner(api, judgeAtItsLimit).RunAsync(StartedRun(1), file, Kept([]));

        Assert.Multiple(() =>
        {
            Assert.That(run.Status, Is.EqualTo(RunStatus.CouldNotFinish));
            Assert.That(run.StoppedBecause, Does.Contain("account").And.Contain("credit balance"));
        });
    }

    /// <summary>The API refusing a session does the same, with the API's reason.</summary>
    [Test]
    public async Task TheApiRefusing_EndsTheRunAsCouldNotFinish_WithTheApisReason()
    {
        var api = new FakeSessionsApi(Answered("Yes."), Refused);
        var kept = new List<EvalRun>();

        var run = await Runner(api).RunAsync(StartedRun(1), TwoConversations, Kept(kept));

        Assert.That(run.Status, Is.EqualTo(RunStatus.CouldNotFinish));
        Assert.That(run.StoppedBecause, Does.Contain("The session is gone."));
        Assert.That(run.Conversations.Select(c => c.Id), Is.EqualTo(new[] { "guide-what-is" }));
        Assert.That(kept[^1], Is.EqualTo(run));
    }

    /// <summary>A cancelled run is a cancellation, never a could not finish with a made-up reason.</summary>
    [Test]
    public void ACancelledRun_LetsTheCancellationThrough()
    {
        var api = new FakeSessionsApi(Answered("Yes."), Answered("Yes."));
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        Assert.CatchAsync<OperationCanceledException>(() =>
            Runner(api).RunAsync(StartedRun(1), TwoConversations, Kept([]), cancellationToken: cancelled.Token));
    }

    /// <summary>F126.3.3: three repeats ask for a conversation's page under three trials, kept in each.</summary>
    [Test]
    public async Task ThreeRepeats_AskForThePage_UnderThreeTrials()
    {
        var api = new FakeSessionsApi(Enumerable.Repeat(Answered("Yes."), 3).ToArray());
        var folder = Directory.CreateTempSubdirectory("runner-pages-").FullName;
        try
        {
            var recordings = new ReplayStore(folder);
            var onePage = new EvalFile(TwoConversations.Agent, [TwoConversations.Conversations[0] with { Page = "/" }]);

            await Runner(api, recordings: recordings).RunAsync(StartedRun(3), onePage, Kept([]));

            Assert.That(api.PagesAsked(), Has.Count.EqualTo(3));
            foreach (int repeat in new[] { 1, 2, 3 })
                Assert.That(await recordings.AsksAsync($"20261123-043012-guide/guide-what-is.{repeat}",
                        ReplayKind.Fetch), Has.Count.EqualTo(1), $"repeat {repeat}");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>
    /// A page the site doesn't have stops the run as could not finish, with the address in its reason, and keeps the
    /// conversations graded before it.
    /// </summary>
    [Test]
    public async Task AMissingPage_StopsTheRun_WithItsAddress_KeepingWhatWasGraded()
    {
        var api = new FakeSessionsApi(Answered("Yes."), Answered("Yes."));
        api.MissingPages.Add("/gone");
        var secondOnAMissingPage = TwoConversations with
        {
            Conversations =
            [TwoConversations.Conversations[0], TwoConversations.Conversations[1] with { Page = "/gone" }]
        };

        var run = await Runner(api).RunAsync(StartedRun(1), secondOnAMissingPage, Kept([]));

        Assert.That(run.Status, Is.EqualTo(RunStatus.CouldNotFinish));
        Assert.That(run.StoppedBecause, Does.Contain("/gone"));
        Assert.That(run.Conversations.Select(c => c.Id), Is.EqualTo(new[] { "guide-what-is" }));
    }

    /// <summary>A run that sent a page says so, and one whose conversations name none doesn't.</summary>
    [Test]
    public async Task ARun_SaysWhetherItSentAPage()
    {
        var withAPage = TwoConversations with
        {
            Conversations =
            [TwoConversations.Conversations[0] with { Page = "/" }, TwoConversations.Conversations[1]]
        };

        var sent = await Runner(new FakeSessionsApi(Answered("Yes."), Answered("Yes.")))
            .RunAsync(StartedRun(1), withAPage, Kept([]));
        var none = await Runner(new FakeSessionsApi(Answered("Yes."), Answered("Yes.")))
            .RunAsync(StartedRun(1), TwoConversations, Kept([]));

        var given = TwoConversations with
        {
            Conversations =
            [
                TwoConversations.Conversations[0] with { GivenPage = new GivenPage("/pricing", "What it costs.") },
                TwoConversations.Conversations[1]
            ]
        };
        var sentGiven = await Runner(new FakeSessionsApi(Answered("Yes."), Answered("Yes.")))
            .RunAsync(StartedRun(1), given, Kept([]));

        Assert.That((sent.PageSent, none.PageSent), Is.EqualTo((true, false)));
        Assert.That(sentGiven.PageSent, Is.True, "F126.5.1: a page given in the file is sent too");
    }

    /// <summary>
    /// F126.4: a replay handed the live run it plays keeps that run's flips where its answers are the same; a run
    /// handed none, as a live run is, keeps the judge's marks as they are.
    /// </summary>
    [Test]
    public async Task AReplayHandedItsLiveRun_KeepsTheLiveRunsFlips_AndOneHandedNoneDoesNot()
    {
        var file = new EvalFile(TwoConversations.Agent,
            [new Conversation("guide-what-is", [new Turn("What is louis-agent?") { Criteria = ["Says it plainly"] }])]);
        var said = new TurnTranscript("What is louis-agent?", "Yes.", [], TimeSpan.FromSeconds(3));
        var flip = new Flip(Started.AddMinutes(-30), "It does.");
        var flipped = new Mark("Says it plainly", false, "Read the answer.") { Flip = flip };
        var liveTurn = new TurnRun(said, [], [flipped], null, null);
        var live = StartedRun(1) with
        {
            Id = "20261123-040000-guide",
            Conversations = [new ConversationRun("guide-what-is", 1, [new RepeatRun(1, [liveTurn])])]
        };
        var replay = StartedRun(1) with
        {
            Kind = RunKind.Replay,
            ReplayOf = live.Id
        };

        var carried = await Runner(new FakeSessionsApi(Answered("Yes.")), NotMet())
            .RunAsync(replay, file, Kept([]), live);
        var none = await Runner(new FakeSessionsApi(Answered("Yes.")), NotMet()).RunAsync(replay, file, Kept([]));

        Assert.That(carried.Conversations[0].Repeats[0].Turns[0].Criteria[0].Flip, Is.EqualTo(flip));
        Assert.That((carried.Passed, none.Passed, none.CountFlips()), Is.EqualTo((1, 0, 0)));
    }

    /// <summary>A judge's model that marks the one criterion it is asked about not met.</summary>
    private static FakeChatClient NotMet() => new(_ =>
        new ChatResponse(new ChatMessage(ChatRole.Assistant, """{"reason":"Read the answer.","met":false}""")));

    /// <summary>The runner over this API and its page route, a judge on this model, a clock at the run's end.</summary>
    private static EvalRunner Runner(FakeSessionsApi api, FakeChatClient? judgeModel = null,
        ReplayStore? recordings = null)
    {
        var judge = new Judge(judgeModel ?? new FakeChatClient());
        var player = new ConversationPlayer(api.Client(), api.Pages(recordings), judge);
        return new EvalRunner(player, new FakeTimeProvider(Ended));
    }

    /// <summary>A run of the guide's eval as it starts, asking for this many repeats.</summary>
    private static EvalRun StartedRun(int repeats) =>
        new("20261123-043012-guide", "guide", "louis-agent-guide", RunKind.Live, repeats, Started, []);

    /// <summary>Keeps each run it is handed in this list.</summary>
    private static Func<EvalRun, Task> Kept(List<EvalRun> kept) => run =>
    {
        kept.Add(run);
        return Task.CompletedTask;
    };
}
