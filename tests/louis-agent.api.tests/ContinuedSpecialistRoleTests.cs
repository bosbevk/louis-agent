using System.Text.Json;
using LouisAgent.Api;
using LouisAgent.Web.Shared;
using LouisAgent.Web.Shared.Services;

namespace LouisAgent.Api.Tests;

// FX17, Louis on a follow-up's card: "Oorgedra aan sub-1 … this must say puls-cook?", with "BESIG" under it where the
// cook's phase belongs. ContinueSubSession names the handle, not the role, so the card had only the handle until the
// answer's footer came. Each session's message_start says its role (session_role: "role" is already Claude's speaker).
public class ContinuedSpecialistRoleTests
{
    [Test]
    public void ASessionsStream_StartsWithItsRole()
    {
        var start = new ClaudeStyleStream("sess_1", ["web", "shops"], "puls-cook").MessageStart();

        using var data = JsonDocument.Parse(start.Data);
        var message = data.RootElement.GetProperty("message");
        Assert.That(message.GetProperty("session_role").GetString(), Is.EqualTo("puls-cook"));
        Assert.That(message.GetProperty("role").GetString(), Is.EqualTo("assistant"), "Claude's speaker, unchanged");
    }

    private static Turn AFollowUpThatStarted()
    {
        var turn = new Turn
        {
            Blocks =
            [
                new Block
                {
                    Kind = BlockKind.Tool, ToolName = Delegation.ContinueToolName, ToolUseId = "toolu_9",
                    ToolInput = """{"handle":"sub-1","message":"**Wil hê:** vir ses mense"}""",
                },
            ],
        };
        var inner = JsonDocument.Parse("""{"type":"message_start","message":{"session_id":"sess_2","role":"assistant","toolsets":["web","shops"],"session_role":"puls-cook"}}""").RootElement;
        var outer = JsonSerializer.SerializeToElement(new { type = "sub_session", tool_use_id = "toolu_9", agent = "puls", @event = "message_start", data = inner });
        TurnStream.Apply(turn, new AgentStreamEvent("sub_session", outer));
        return turn;
    }

    [Test]
    public void AFollowUpsCard_NamesTheRole_WhileItWorks() =>
        Assert.That(Delegation.From(AFollowUpThatStarted().Blocks[0])!.Role, Is.EqualTo("puls-cook"));

    [Test]
    public void AFollowUpsLine_SaysWhatTheCookIsDoing()
    {
        var text = new ChatText { Processing = "BESIG", RolePhases = new Dictionary<string, RolePhases> { ["puls-cook"] = new("Soek bestanddele", "Bou resep") } };

        Assert.That(WorkStatus.Label(AFollowUpThatStarted(), text), Is.EqualTo("Soek bestanddele"));
    }
}
