using LouisAgent.Core.Sessions.Replay.Store;
using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Transcripts;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Runs;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Evals.Tests;

/// <summary>
/// The proof the judge's replay rests on: the engine's replay client serves the judge's typed call, recording it once
/// and giving the same mark back with the model never called again, and refusing a criterion it never heard.
/// </summary>
public class JudgeReplayTests
{
    private const string Criterion = "Says it is a framework";

    private static readonly IReadOnlyList<TurnTranscript> Conversation =
        [new("What is louis-agent?", "An AI agent framework in C#.", [], TimeSpan.FromSeconds(4))];

    private static readonly RunTrial Live = new("20261123-043012-guide", "guide-what-is", 1);
    private static readonly RunTrial Replay = new("20261123-050000-guide", "guide-what-is", 1, Live.Run);

    private string _folder = "";
    private int _calls;

    /// <summary>A recordings folder of this test's own, and no call yet.</summary>
    [SetUp]
    public void MakeFolder()
    {
        _folder = Directory.CreateTempSubdirectory("judge-replay-").FullName;
        _calls = 0;
    }

    /// <summary>The folder goes when the test is done.</summary>
    [TearDown]
    public void RemoveFolder() => Directory.Delete(_folder, recursive: true);

    /// <summary>While recording, the judge marks the criterion and the model is called once.</summary>
    [Test]
    public async Task WhileRecording_TheJudgeMarks_AndTheModelIsCalledOnce()
    {
        var mark = await MarkAsync(Live, Criterion);

        Assert.That((mark.Met, _calls), Is.EqualTo((true, 1)));
    }

    /// <summary>In replay the same criterion gives the same mark back, and the model isn't called again.</summary>
    [Test]
    public async Task InReplay_TheSameCriterion_GivesTheSameMarkBack_WithNoCall()
    {
        var recorded = await MarkAsync(Live, Criterion);

        var replayed = await MarkAsync(Replay, Criterion);

        Assert.That(replayed, Is.EqualTo(recorded));
        Assert.That(_calls, Is.EqualTo(1));
    }

    /// <summary>A criterion with other words, in replay, can't be marked.</summary>
    [Test]
    public async Task InReplay_ACriterionWithOtherWords_CantBeMarked()
    {
        await MarkAsync(Live, Criterion);

        Assert.ThrowsAsync<JudgeFailedException>(() => MarkAsync(Replay, "Says it is written in Rust"));
        Assert.That(_calls, Is.EqualTo(1));
    }

    /// <summary>The judge, on a model that counts its calls, wrapped for replay, marking within this trial.</summary>
    private async Task<Mark> MarkAsync(RunTrial trial, string criterion)
    {
        var model = new FakeChatClient(Enumerable.Repeat<Func<IList<ChatMessage>, ChatResponse>>(_ =>
        {
            _calls++;
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, """{"reason":"It says so.","met":true}"""));
        }, 3).ToArray());
        var judge = new Judge(new JudgeReplay(model, new ReplayStore(_folder)));

        using var replay = JudgeReplay.Begin(trial);
        return await judge.MarkAsync(criterion, Conversation);
    }
}
