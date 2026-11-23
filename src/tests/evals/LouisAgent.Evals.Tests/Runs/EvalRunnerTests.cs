using LouisAgent.Composition.Agents;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Files;
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
            Runner(api).RunAsync(StartedRun(1), TwoConversations, Kept([]), cancelled.Token));
    }

    /// <summary>The runner over this API, a judge on this model, and a clock at the run's end time.</summary>
    private static EvalRunner Runner(FakeSessionsApi api, FakeChatClient? judgeModel = null)
    {
        var judge = new Judge(judgeModel ?? new FakeChatClient());
        return new EvalRunner(new ConversationPlayer(api.Client(), judge), new FakeTimeProvider(Ended));
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
