using System.Text.Json;
using LouisAgent.SessionStream;
using LouisAgent.Web.Chats.Replay;

namespace LouisAgent.Web.Tests.Chats.Replay;

/// <summary>
/// A recorded stream carries no times, so the replay paces each event by its kind (F120): the answer types at reading
/// speed, thinking runs by faster, and the agent's tool calls and their results take a beat.
/// </summary>
public class RecordingPaceTests
{
    /// <summary>An event as the stream's reader hands it over.</summary>
    private static SessionEvent Event(string type, string json) =>
        SessionEvent.From(type, JsonDocument.Parse(json).RootElement);

    private static SessionEvent Started(string block) =>
        Event("content_block_start", $$"""{"content_block":{{block}}}""");

    private static SessionEvent Delta(string delta) => Event("content_block_delta", $$"""{"delta":{{delta}}}""");

    /// <summary>Thinking goes by faster than the answer, which a visitor reads.</summary>
    [Test]
    public void Thinking_RunsFasterThanTheAnswer()
    {
        int thinking = RecordingPace.WaitMs(Delta("""{"type":"thinking_delta","thinking":"a few words"}"""));
        int text = RecordingPace.WaitMs(Delta("""{"type":"text_delta","text":"a few words"}"""));

        Assert.Multiple(() =>
        {
            Assert.That(thinking, Is.GreaterThan(0));
            Assert.That(text, Is.GreaterThan(thinking));
        });
    }

    /// <summary>
    /// A tool call and its result each take a beat longer than a word, so a visitor sees the agent act.
    /// </summary>
    [Test]
    public void ATool_TakesABeat()
    {
        int word = RecordingPace.WaitMs(Delta("""{"type":"text_delta","text":"word"}"""));
        int call = RecordingPace.WaitMs(Started("""{"type":"tool_use","id":"t1","name":"ReadFile","input":{}}"""));
        int result = RecordingPace.WaitMs(
            Started("""{"type":"tool_result","tool_use_id":"t1","content":"x","is_error":false}"""));

        Assert.Multiple(() =>
        {
            Assert.That(call, Is.GreaterThan(word * 5));
            Assert.That(result, Is.GreaterThan(word * 5));
        });
    }

    /// <summary>What draws nothing new (a block's end, the message's end) doesn't wait.</summary>
    [Test]
    public void WhatDrawsNothing_DoesntWait()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RecordingPace.WaitMs(Event("content_block_stop", "{}")), Is.Zero);
            Assert.That(RecordingPace.WaitMs(Event("message_stop", "{}")), Is.Zero);
        });
    }

    /// <summary>The steps keep the events in order, each with its own wait.</summary>
    [Test]
    public void Steps_KeepTheOrder()
    {
        var events = new[] { Delta("""{"type":"text_delta","text":"a"}"""), Event("message_stop", "{}") };

        var steps = RecordingPace.Steps(events);

        Assert.That(steps.Select(step => step.Event), Is.EqualTo(events));
        Assert.That(steps.Select(step => step.WaitMs), Is.EqualTo(events.Select(RecordingPace.WaitMs)));
    }

    /// <summary>The pace scales every wait: half as long at 0.5, none at all at 0.</summary>
    [Test]
    public void ThePace_ScalesTheWaits()
    {
        var events = new[] { Started("""{"type":"tool_use","id":"t1","name":"ReadFile","input":{}}""") };
        int full = RecordingPace.WaitMs(events[0]);

        Assert.Multiple(() =>
        {
            Assert.That(RecordingPace.Steps(events, 0.5).Single().WaitMs, Is.EqualTo(full / 2));
            Assert.That(RecordingPace.Steps(events, 0).Single().WaitMs, Is.Zero);
        });
    }
}
