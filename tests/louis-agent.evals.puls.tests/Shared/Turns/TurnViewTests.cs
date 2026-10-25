using LouisAgent.SessionStream;
using LouisAgent.Evals.Puls.Shared.Turns;

namespace LouisAgent.Evals.Puls.Tests.Shared.Turns;

// The turn reader (the Python's read_turn and hosts), read from the session's typed events: the answer, the calls, the role,
// and the hosts a turn read. Added with 3.2, since the cook scores read the turn it makes.
public class TurnViewTests
{
    [Test]
    public void ATurnsEvents_AreReadAsItsAnswerCallsRoleAndResults()
    {
        var events = new[]
        {
            SessionEvent.From("content_block_start", Json("""{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t1","name":"StartSubSession","input":{"role":"puls-cook"}}}""")),
            SessionEvent.From("content_block_start", Json("""{"type":"content_block_start","index":1,"content_block":{"type":"tool_result","tool_use_id":"t1","content":"Die resep."}}""")),
            SessionEvent.From("content_block_delta", Json("""{"type":"content_block_delta","index":2,"delta":{"type":"text_delta","text":" Hallo "}}""")),
        };

        var turn = TurnView.From(events);

        Assert.That(turn.Answer, Is.EqualTo("Hallo"));
        Assert.That(turn.Role, Is.EqualTo("puls-cook"));
        Assert.That(turn.Calls.Single().Name, Is.EqualTo("StartSubSession"));
        Assert.That(turn.Results, Is.EqualTo(new[] { "Die resep." }));
    }

    [Test]
    public void ASpecialistsCall_IsItsSpecialistCall_AndItsHostIsRead()
    {
        var events = new[]
        {
            SessionEvent.From("sub_session", Json("""{"type":"sub_session","tool_use_id":"t1","agent":"puls-cook","event":"content_block_start","data":{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t2","name":"FetchUrl","input":{"url":"https://www.checkers.co.za/p/1"}}}}""")),
        };

        var turn = TurnView.From(events);

        Assert.That(turn.Calls, Is.Empty);
        Assert.That(turn.SpecialistCalls.Single().Name, Is.EqualTo("FetchUrl"));
        Assert.That(ToolHosts.Of(turn.SpecialistCalls), Is.EqualTo(new[] { "checkers.co.za" }));
    }

    [Test]
    public void AnErrorInASpecialist_IsNotTheTurnsError()
    {
        // The Python reads a turn's error from its own events only; a specialist's error is its own, shown in its card.
        var events = new[]
        {
            SessionEvent.From("sub_session", Json("""{"type":"sub_session","tool_use_id":"t1","agent":"puls-cook","event":"error","data":{"type":"error","error":{"type":"api_error","message":"the specialist failed"}}}""")),
        };

        Assert.That(TurnView.From(events).Error, Is.Null);
    }

    [Test]
    public void AToolResultWhoseContentIsNotAString_IsReadAsItsText()
    {
        var events = new[]
        {
            SessionEvent.From("content_block_start", Json("""{"type":"content_block_start","index":1,"content_block":{"type":"tool_result","tool_use_id":"t1","content":[{"type":"text","text":"Die resep."}]}}""")),
        };

        Assert.That(TurnView.From(events).Results.Single(), Does.Contain("Die resep."));
    }

    [Test]
    public void AnErrorThatIsAString_IsTheTurnsError_ReadAsItsJson()
    {
        var events = new[] { SessionEvent.From("error", Json("""{"type":"error","error":"the connection dropped"}""")) };

        Assert.That(TurnView.From(events).Error, Does.Contain("the connection dropped"));
    }

    [Test]
    public void AnEmptyRole_IsNoRole_AndTheNextStartIsTheRole()
    {
        // The Python's `role or personality`, and `is None`: an empty role is no role.
        var events = new[]
        {
            SessionEvent.From("content_block_start", Json("""{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t1","name":"StartSubSession","input":{"role":""}}}""")),
            SessionEvent.From("content_block_start", Json("""{"type":"content_block_start","index":1,"content_block":{"type":"tool_use","id":"t2","name":"StartSubSession","input":{"role":"puls-cook"}}}""")),
        };

        Assert.That(TurnView.From(events).Role, Is.EqualTo("puls-cook"));
    }

    private static System.Text.Json.JsonElement Json(string text) => System.Text.Json.JsonDocument.Parse(text).RootElement.Clone();
}
