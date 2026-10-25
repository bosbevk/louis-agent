using System.Text.Json;
using LouisAgent.Evals.Puls.Turns;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Puls.Tests.Turns;

// The two readers of a turn's answer keep one rule until step 5 makes TurnView build on EngineEvents (F18 step 4.4 review): the same
// events give the same answer, and a sub-session's text and an odd delta are left out by both. Written first: AnswerOf threw
// NotImplementedException on the red run.
public class AnswerReaderParityTests
{
    [Test]
    public void EngineEventsAndTurnViewGiveTheSameAnswer()
    {
        var events = new[]
        {
            Event("content_block_delta", """{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":" Hallo "}}"""),
            Event("content_block_delta", """{"type":"content_block_delta","index":0,"delta":{"text":"geen tipe"}}"""),
            Event("content_block_delta", """{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":42}}"""),
            Event("sub_session", """{"tool_use_id":"t0","agent":"puls","event":"content_block_delta","data":{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Spesialis"}}}"""),
            Event("content_block_delta", """{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"wêreld "}}"""),
        };

        string fromEngine = EngineEvents.AnswerOf(new TurnTranscript(1, "sess_1", "Hallo", events, TimeSpan.FromSeconds(1), null));

        Assert.That(fromEngine, Is.EqualTo(TurnView.From(events).Answer));
        Assert.That(fromEngine, Is.EqualTo("Hallo wêreld"));
    }

    private static SessionEvent Event(string type, string json) => SessionEvent.From(type, JsonDocument.Parse(json).RootElement.Clone());
}
