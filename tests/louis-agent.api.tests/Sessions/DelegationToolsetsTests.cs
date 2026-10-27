using LouisAgent.Testing;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Chats;
using ClaudeStreamFormat = LouisAgent.Api.Sessions.Streaming.Claude.ClaudeStreamFormat;
using System.Text.Json;
using LouisAgent.SessionStream;
using LouisAgent.Web.Shared;

namespace LouisAgent.Api.Tests.Sessions;

// F34, Louis's screenshot of the cook's card: its chips said `web`, the toolsets Puls sent, while the cook also had
// `shops`, which its role adds on the peer. Each session's message_start says what it loaded; the card shows that.
public class DelegationToolsetsTests
{
    [Test]
    public void ASessionsStream_StartsWithTheToolsetsItLoaded()
    {
        var start = new ClaudeStreamFormat("sess_1", [SampleToolset.Alpha, SampleToolset.Beta]).Start();

        using var data = JsonDocument.Parse(start.Data);
        Assert.That(data.RootElement.GetProperty("message").GetProperty("toolsets").EnumerateArray().Select(t => t.GetString()),
            Is.EqualTo(new[] { SampleToolset.Alpha, SampleToolset.Beta }));
    }

    private static Block Cook(string? footer = null) => new()
    {
        Kind = BlockKind.Tool, ToolName = Delegation.ToolName, ToolUseId = "toolu_1",
        ToolInput = SampleToolset.Fill(SampleRole.Fill("""{"toolsets":["<Alpha>"],"message":"**Vir:** kok","role":"<RoleAlpha>"}""")),
        ToolResult = footer is null ? null : "Die resep.\n\n" + footer,
    };

    [Test]
    public void TheCard_ShowsWhatTheSpecialistLoaded_WhileItWorks()
    {
        var turn = new Turn { Blocks = [Cook()] };
        var inner = JsonDocument.Parse(SampleToolset.Fill("""{"type":"message_start","message":{"session_id":"sess_2","role":"assistant","toolsets":["<Alpha>","<Beta>"]}}""")).RootElement;
        var outer = JsonSerializer.SerializeToElement(new { type = "sub_session", tool_use_id = "toolu_1", agent = "puls", @event = "message_start", data = inner });

        TurnStream.Apply(turn, SessionEvent.From("sub_session", outer));

        Assert.That(Delegation.From(turn.Blocks[0])!.Toolsets, Is.EqualTo(new[] { SampleToolset.Alpha, SampleToolset.Beta }));
    }

    [Test]
    public void TheCard_ShowsTheFootersToolsets_OnceItsDone() =>
        Assert.That(Delegation.From(Cook(SampleToolset.Fill(SampleRole.Fill("[Sub-session sub-1 on puls (<Alpha>, <Beta>; <RoleAlpha>): 3 requests, 1 USD]"))))!.Toolsets,
            Is.EqualTo(new[] { SampleToolset.Alpha, SampleToolset.Beta }));

    [Test]
    public void AnOlderChat_ShowsTheToolsetsSent() =>
        Assert.That(Delegation.From(Cook())!.Toolsets, Is.EqualTo(new[] { SampleToolset.Alpha }));
}
