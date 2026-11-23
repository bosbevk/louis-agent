using LouisAgent.Evals.Contracts.Transcripts;

namespace LouisAgent.Evals.Tests.Transcripts;

/// <summary>
/// F126.4: when two turns say the same thing, which a re-grade and a replay both ask before a mark carries over. The
/// ask, the answer, the error and every tool call count; the time the turn took doesn't.
/// </summary>
public class SaysTheSameTests
{
    private static readonly TurnTranscript Turn = new("What is louis-agent?", "A framework.",
        [new ToolCallTranscript("ReadDoc", """{"path":"README.md"}""", "# louis-agent")], TimeSpan.FromSeconds(7),
        "The model is overloaded.");

    /// <summary>
    /// The same ask, answer, error and calls say the same, however long each took, and with their calls in lists of
    /// their own: a record's own equality would compare the lists by reference and say they differ.
    /// </summary>
    [Test]
    public void TheSameTurn_SaysTheSame_WhateverItTook_AndWithCallsInAListOfItsOwn()
    {
        var again = Turn with
        {
            Took = TimeSpan.FromSeconds(30),
            Calls = [.. Turn.Calls]
        };

        var copiedCallsOnly = Turn with { Calls = [.. Turn.Calls] };

        Assert.That(again.SaysTheSameAs(Turn), Is.True);
        Assert.That(copiedCallsOnly, Is.Not.EqualTo(Turn), "a record's equality compares the lists by reference");
        Assert.That(copiedCallsOnly.SaysTheSameAs(Turn), Is.True);
    }

    /// <summary>One changed word in the ask, answer, error or a call, a call more, or one that failed, differs.</summary>
    [Test]
    public void AChangedWordOrCall_SaysSomethingElse()
    {
        var call = Turn.Calls[0];
        TurnTranscript[] changed =
        [
            Turn with { Ask = "What is louis-agent, then?" },
            Turn with { Answer = "A framework in C#." },
            Turn with { Error = null },
            Turn with { Calls = [call with { Tool = "ReadSkill" }] },
            Turn with { Calls = [call with { Input = """{"path":"docs/README.md"}""" }] },
            Turn with { Calls = [call with { Result = "# Docs" }] },
            Turn with { Calls = [call with { Failed = true }] },
            Turn with { Calls = [call, call] }
        ];

        Assert.That(changed.Select(turn => turn.SaysTheSameAs(Turn)), Has.All.False);
    }
}
