using LouisAgent.Puls.Admin.LiveRuns;

namespace LouisAgent.Puls.Tests.Admin.LiveRuns;

/// <summary>
/// A line of the run's script that starts with <c>@</c> is one of its events; anything that isn't a JSON object stays a
/// plain line of the log.
/// </summary>
public class RunEventTests
{
    /// <summary>A turn's event is read with its figures.</summary>
    [Test]
    public void AnEvent_IsReadWithItsFigures()
    {
        var read = RunEvent.TryRead(SampleMode.Fill("""
                                                    {"event":"turn","mode":"<ModeAlpha>","repeat":2,"task":"bobotie","turn":3,"role":"r","continued":"sub-1",
                                                     "puls_calls":1,"specialist_calls":4,"seconds":2.5,"error":null}
                                                    """));

        Assert.That(read, Is.Not.Null);
        Assert.That(read!.Event, Is.EqualTo("turn"));
        Assert.That(read.Key, Is.EqualTo(new TurnKey(SampleMode.Alpha, 2, "bobotie", 3)));
        Assert.That((read.Role, read.Continued, read.PulsCalls, read.SpecialistCalls, read.Seconds),
            Is.EqualTo(("r", "sub-1", 1, 4, 2.5)));
    }

    /// <summary>An event that names no repeat or turn is the first of each.</summary>
    [Test]
    public void AnEventWithoutRepeatOrTurn_IsTheFirst()
    {
        var read = RunEvent.TryRead("""{"event":"question","task":"chat","text":"Hallo"}""");

        Assert.That(read!.Key, Is.EqualTo(new TurnKey("", 1, "chat", 1)));
        Assert.That(read.Text, Is.EqualTo("Hallo"));
    }

    /// <summary>The event's kind is read once into its enum; a kind the page doesn't know is none.</summary>
    [TestCase("question", RunEventKind.Question)]
    [TestCase("sse", RunEventKind.Sse)]
    [TestCase("turn", RunEventKind.Turn)]
    [TestCase("conversation", RunEventKind.Conversation)]
    [TestCase("start", null)]
    public void TheKind_IsAnEnum(string kind, RunEventKind? expected)
    {
        var read = RunEvent.TryRead($$"""{"event":"{{kind}}"}""");

        Assert.That(read!.Kind, Is.EqualTo(expected));
    }

    /// <summary>Broken JSON, or JSON that isn't an object, is no event.</summary>
    [TestCase("{ not json")]
    [TestCase("[1,2]")]
    [TestCase("\"text\"")]
    public void WhatIsNoObject_IsNoEvent(string line)
    {
        Assert.That(RunEvent.TryRead(line), Is.Null);
    }
}
