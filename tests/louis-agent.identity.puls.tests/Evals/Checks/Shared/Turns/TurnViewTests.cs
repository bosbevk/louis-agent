using LouisAgent.Choices;
using LouisAgent.Identities.Puls.Evals.Checks.Shared;
using LouisAgent.Identities.Puls.Evals.Checks.Shared.Turns;
using LouisAgent.SessionStream;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks.Shared.Turns;

/// <summary>
/// Reading a turn from the session's typed events, as the old Python did: the answer, the calls, the role, and the
/// hosts a turn read. The cook's checks read the turn this makes.
/// </summary>
public class TurnViewTests
{
    /// <summary>A turn's events are read as its answer, its calls, its role and its tool results.</summary>
    [Test]
    public void ATurnsEvents_AreReadAsItsAnswerCallsRoleAndResults()
    {
        var events = new[]
        {
            SessionEvent.From("content_block_start",
                Json(
                    """{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t1","name":"StartSubSession","input":{"role":"puls-cook"}}}""")),
            SessionEvent.From("content_block_start",
                Json(
                    """{"type":"content_block_start","index":1,"content_block":{"type":"tool_result","tool_use_id":"t1","content":"Die resep."}}""")),
            SessionEvent.From("content_block_delta",
                Json("""{"type":"content_block_delta","index":2,"delta":{"type":"text_delta","text":" Hallo "}}""")),
        };

        var turn = TurnView.From(events);

        Assert.That(turn.Answer, Is.EqualTo("Hallo"));
        Assert.That(turn.Role, Is.EqualTo(PulsEvalRole.Cook.Wire()));
        Assert.That(turn.Calls.Single().Name, Is.EqualTo("StartSubSession"));
        Assert.That(turn.Results, Is.EqualTo(new[] { "Die resep." }));
    }

    /// <summary>A specialist's tool call is its own, not the turn's, and its host is read.</summary>
    [Test]
    public void ASpecialistsCall_IsItsSpecialistCall_AndItsHostIsRead()
    {
        var events = new[]
        {
            SessionEvent.From("sub_session",
                Json(
                    """{"type":"sub_session","tool_use_id":"t1","agent":"puls-cook","event":"content_block_start","data":{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t2","name":"FetchUrl","input":{"url":"https://www.checkers.co.za/p/1"}}}}""")),
        };

        var turn = TurnView.From(events);

        Assert.That(turn.Calls, Is.Empty);
        Assert.That(turn.SpecialistCalls.Single().Name, Is.EqualTo("FetchUrl"));
        Assert.That(ToolHosts.Of(turn.SpecialistCalls), Is.EqualTo(new[] { "checkers.co.za" }));
    }

    /// <summary>An error inside a specialist is the specialist's, not the turn's.</summary>
    [Test]
    public void AnErrorInASpecialist_IsNotTheTurnsError()
    {
        // The Python reads a turn's error from its own events only; a specialist's error is its own, shown in its card.
        var events = new[]
        {
            SessionEvent.From("sub_session",
                Json(
                    """{"type":"sub_session","tool_use_id":"t1","agent":"puls-cook","event":"error","data":{"type":"error","error":{"type":"api_error","message":"the specialist failed"}}}""")),
        };

        Assert.That(TurnView.From(events).Error, Is.Null);
    }

    /// <summary>A tool result whose content isn't a string is still read, as its text.</summary>
    [Test]
    public void AToolResultWhoseContentIsNotAString_IsReadAsItsText()
    {
        var events = new[]
        {
            SessionEvent.From("content_block_start",
                Json(
                    """{"type":"content_block_start","index":1,"content_block":{"type":"tool_result","tool_use_id":"t1","content":[{"type":"text","text":"Die resep."}]}}""")),
        };

        Assert.That(TurnView.From(events).Results.Single(), Does.Contain("Die resep."));
    }

    /// <summary>An error given as a plain string is the turn's error, read as its JSON.</summary>
    [Test]
    public void AnErrorThatIsAString_IsTheTurnsError_ReadAsItsJson()
    {
        var events = new[]
        {
            SessionEvent.From("error", Json("""{"type":"error","error":"the connection dropped"}"""))
        };

        Assert.That(TurnView.From(events).Error, Does.Contain("the connection dropped"));
    }

    /// <summary>An empty role is no role, so the next sub-session's role is the turn's.</summary>
    [Test]
    public void AnEmptyRole_IsNoRole_AndTheNextStartIsTheRole()
    {
        // The Python's `role or personality`, and `is None`: an empty role is no role.
        var events = new[]
        {
            SessionEvent.From("content_block_start",
                Json(
                    """{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t1","name":"StartSubSession","input":{"role":""}}}""")),
            SessionEvent.From("content_block_start",
                Json(
                    """{"type":"content_block_start","index":1,"content_block":{"type":"tool_use","id":"t2","name":"StartSubSession","input":{"role":"puls-cook"}}}""")),
        };

        Assert.That(TurnView.From(events).Role, Is.EqualTo(PulsEvalRole.Cook.Wire()));
    }

    /// <summary>A JSON value from its text.</summary>
    private static System.Text.Json.JsonElement Json(string text) =>
        System.Text.Json.JsonDocument.Parse(text).RootElement.Clone();
}
