using System.Globalization;
using LouisAgent.Composition.Messages;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Messages.Holes.Grading;
using LouisAgent.Evals.Transcripts;

namespace LouisAgent.Evals.Grading;

/// <summary>
/// Tells a fact about a turn from its transcript, in code: free, instant, and the same every time. Each mark carries
/// one plain line of what code saw, met or not, so a red mark explains itself.
/// </summary>
/// <remarks>
/// One switch on the fact's kind (ADR 0024), each arm reading the transcript and nothing else. A tool that was called
/// and failed still counts as used: the fact says the agent reached for it, and whether the turn went well is
/// <c>no-error</c>'s and the judge's to say.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0134-an-eval-is-one-file-of-conversations.md">ADR 0134: an eval is one file of conversations</seealso>
public static class Facts
{
    /// <summary>
    /// The fact's mark for this turn; <paramref name="earlier"/> are the conversation's turns before it.
    /// </summary>
    public static Mark Tell(Fact fact, TurnTranscript turn, IReadOnlyList<TurnTranscript> earlier) => fact.Kind switch
    {
        FactKind.NoError => NoError(fact, turn),
        FactKind.Used => Used(fact, turn, called: true),
        FactKind.NotUsed => Used(fact, turn, called: false),
        FactKind.Under => Under(fact, turn),
        FactKind.NeverSays => NeverSays(fact, turn),
        _ => throw new NotSupportedException(fact.ToString())
    };

    /// <summary>Met when the turn ended without an error; the reason holds the error when it didn't.</summary>
    private static Mark NoError(Fact fact, TurnTranscript turn)
    {
        if (turn.Error is null) return new Mark(fact.ToString(), true, MessageText.Format(FactMessage.EndedWell));

        var ended = new EndedWithErrorHoles(turn.Error);
        return new Mark(fact.ToString(), false, MessageText.Format(FactMessage.EndedWithError, ended));
    }

    /// <summary>
    /// Whether the turn called the tool, its name's case ignored, held against <paramref name="called"/>: met for
    /// <c>used</c> when it did, for <c>not-used</c> when it didn't. The reason lists the tools in the order called.
    /// </summary>
    private static Mark Used(Fact fact, TurnTranscript turn, bool called)
    {
        bool did = turn.Calls.Any(call => string.Equals(call.Tool, fact.Argument, StringComparison.OrdinalIgnoreCase));
        return new Mark(fact.ToString(), did == called, ToolsCalled(turn));
    }

    /// <summary>The tools the turn called, in order, or that none was.</summary>
    private static string ToolsCalled(TurnTranscript turn)
    {
        if (turn.Calls.Count == 0) return MessageText.Format(FactMessage.NoToolCalled);

        var tools = new ToolsCalledHoles(string.Join(", ", turn.Calls.Select(call => call.Tool).Distinct()));
        return MessageText.Format(FactMessage.ToolsCalled, tools);
    }

    /// <summary>Met when the turn took less than the fact's whole seconds.</summary>
    private static Mark Under(Fact fact, TurnTranscript turn)
    {
        var limit = TimeSpan.FromSeconds(int.Parse(fact.Argument, CultureInfo.InvariantCulture));
        var took = new TookHoles(turn.Took.TotalSeconds.ToString("0.#", CultureInfo.InvariantCulture));
        return new Mark(fact.ToString(), turn.Took < limit, MessageText.Format(FactMessage.Took, took));
    }

    /// <summary>
    /// Met when the answer as the agent sent it doesn't hold the text, the case ignored. The answer is markdown, so a
    /// link's or an image's address is read too: the leak OWASP's LLM01 describes.
    /// </summary>
    private static Mark NeverSays(Fact fact, TurnTranscript turn)
    {
        bool holds = turn.Answer.Contains(fact.Argument, StringComparison.OrdinalIgnoreCase);
        var text = new AnswerTextHoles(fact.Argument);
        var said = holds ? FactMessage.AnswerHolds : FactMessage.AnswerDoesntHold;
        return new Mark(fact.ToString(), !holds, MessageText.Format(said, text));
    }
}
