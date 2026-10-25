using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Trials;

// The turn's own answer (F18 step 4.4 review): its text deltas joined in order and trimmed, as TurnView.Answer reads them. Written
// first: AnswerOf threw NotImplementedException on the red run.
public class AnswerOfTests
{
    [Test]
    public void TheAnswerIsItsTextDeltasJoinedInOrderAndTrimmed()
    {
        var turn = Turn(1, [Text(" Hallo "), Text("wêreld ")]);

        Assert.That(EngineEvents.AnswerOf(turn), Is.EqualTo("Hallo wêreld"));
    }

    [Test]
    public void ASubSessionsTextIsNotTheAnswer()
    {
        var specialist = SessionEvent.From("sub_session", Json("""{"tool_use_id":"t0","agent":"puls","event":"content_block_delta","data":{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Spesialis"}}}"""));
        var turn = Turn(1, [Text("Antwoord"), specialist]);

        Assert.That(EngineEvents.AnswerOf(turn), Is.EqualTo("Antwoord"));
    }

    [Test]
    public void AnOddDeltaIsSkipped()
    {
        // A delta with no type, one whose text isn't a string, and one that isn't an object: none throws, and none is the answer.
        var turn = Turn(1,
        [
            Delta("""{"type":"content_block_delta","index":0,"delta":{"text":"geen tipe"}}"""),
            Delta("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":42}}"""),
            Delta("""{"type":"content_block_delta","index":0,"delta":"text"}"""),
            Text("Ja"),
        ]);

        Assert.That(EngineEvents.AnswerOf(turn), Is.EqualTo("Ja"));
    }

    [Test]
    public void ATurnWithNoTextHasAnEmptyAnswer() =>
        Assert.That(EngineEvents.AnswerOf(Turn(1, [Usage("0.1")])), Is.EqualTo(""));

    private static SessionEvent Delta(string json) => SessionEvent.From("content_block_delta", Json(json));
}
