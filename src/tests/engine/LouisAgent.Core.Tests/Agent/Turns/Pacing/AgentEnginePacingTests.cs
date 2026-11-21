using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Core.Tests.Agent.Turns.Pacing;

/// <summary>
/// F155: the engine paces a turn's text when its host turns <c>STREAM_PACE</c> on, and leaves the stream as it was when
/// it is off, the default; the history holds the whole answer either way, since the pacing is outside the turn.
/// </summary>
public class AgentEnginePacingTests
{
    /// <summary>Sixty words: 300 letters, which the fake model sends as one piece.</summary>
    private static readonly string Answer = string.Concat(Enumerable.Repeat("word ", 60)).TrimEnd();

    /// <summary>An engine on the fake model, answering with <see cref="Answer"/>, pacing or not, on this clock.</summary>
    private static AgentEngine Engine(bool paced, TimeProvider clock)
    {
        var options = TestPaths.Agent();
        options.StreamPace = paced;
        var model = new FakeChatClient(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, Answer)));
        return new AgentEngine(new MarkdownSkillProvider(TestPaths.SampleSkills), model, options, clock: clock);
    }

    /// <summary>The text pieces of one turn, read to its end, the fake clock moved a frame at a time while it waits.</summary>
    private static async Task<List<string>> TextPiecesAsync(AgentEngine engine, List<ChatMessage> history,
        FakeTimeProvider clock)
    {
        var pieces = new List<string>();
        await using var turn = engine.StreamPromptAsync(history, "say it").GetAsyncEnumerator();
        while (true)
        {
            var next = turn.MoveNextAsync().AsTask();
            while (!next.IsCompleted)
            {
                await Task.Delay(1);
                if (!next.IsCompleted) clock.Advance(TimeSpan.FromMilliseconds(33));
            }

            if (!await next) return pieces;
            if (turn.Current.Text is { Length: > 0 } text) pieces.Add(text);
        }
    }

    /// <summary>With STREAM_PACE on, the answer comes out in pieces over time, and the history holds it whole.</summary>
    [Test]
    public async Task On_TheTextComesOutPaced_AndTheHistoryHoldsItWhole()
    {
        var clock = new FakeTimeProvider();
        var engine = Engine(paced: true, clock);
        var history = engine.StartHistory();

        var pieces = await TextPiecesAsync(engine, history, clock);

        Assert.Multiple(() =>
        {
            Assert.That(pieces, Has.Count.GreaterThan(5));
            Assert.That(string.Concat(pieces), Is.EqualTo(Answer));
            Assert.That(history[^1].Text, Is.EqualTo(Answer));
        });
    }

    /// <summary>With it off, the turn's stream is as it was: the answer in the one piece the model sent.</summary>
    [Test]
    public async Task Off_TheStreamIsTheTurns()
    {
        var clock = new FakeTimeProvider();
        var engine = Engine(paced: false, clock);
        var history = engine.StartHistory();

        var pieces = await TextPiecesAsync(engine, history, clock);

        Assert.Multiple(() =>
        {
            Assert.That(pieces, Is.EqualTo(new[] { Answer }));
            Assert.That(history[^1].Text, Is.EqualTo(Answer));
        });
    }

    /// <summary>STREAM_PACE on turns it on, in any case; anything else, or nothing, leaves it off.</summary>
    [TestCase("on", true)]
    [TestCase("ON", true)]
    [TestCase("off", false)]
    [TestCase(null, false)]
    public void TheSetting_IsReadFromStreamPace(string? value, bool paced) =>
        Assert.That(AgentOptions.FromEnvironment(name => name == "STREAM_PACE" ? value : null).StreamPace,
            Is.EqualTo(paced));
}
