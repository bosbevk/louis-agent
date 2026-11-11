using ClaudeStreamFormat = LouisAgent.Api.Sessions.Streaming.Claude.ClaudeStreamFormat;
using System.Text.Json;
using LouisAgent.SessionStream;
using LouisAgent.Web.Chats;

namespace LouisAgent.Api.Tests.Sessions;

/// <summary>
/// A delegation card shows the toolsets the asked agent actually loaded (F34). From Louis's screenshot: the cook's
/// chips said `web`, though its role also adds `shops` on the peer. An ask names only an address (F127.3.1), so the
/// card reads them from the child: each session's message_start says what it loaded.
/// </summary>
public class DelegationToolsetsTests
{
    /// <summary>A session's stream opens by naming the toolsets it loaded.</summary>
    [Test]
    public void ASessionsStream_StartsWithTheToolsetsItLoaded()
    {
        var start = new ClaudeStreamFormat("sess_1", [SampleToolset.Alpha, SampleToolset.Beta]).Start();

        using var data = JsonDocument.Parse(start.Data);
        Assert.That(
            data.RootElement.GetProperty("message").GetProperty("toolsets").EnumerateArray()
                .Select(t => t.GetString()),
            Is.EqualTo(new[] { SampleToolset.Alpha, SampleToolset.Beta }));
    }

    /// <summary>
    /// The cook asked by its address: an ask names no toolsets, so the card learns them from the child.
    /// </summary>
    private static Block Cook() => new()
    {
        Kind = BlockKind.Tool,
        ToolName = Delegation.AskToolName,
        ToolUseId = "toolu_1",
        ToolInput = """{"address":"acme.cook","message":"**Vir:** kok"}"""
    };

    /// <summary>
    /// While the asked agent works, its own stream's start says what it loaded, and the card shows that.
    /// </summary>
    [Test]
    public void TheCard_ShowsWhatTheAskedAgentLoaded_WhileItWorks()
    {
        var turn = new Turn { Blocks = [Cook()] };
        var inner = JsonDocument
            .Parse(SampleToolset.Fill(
                """{"type":"message_start","message":{"session_id":"sess_2","role":"assistant","toolsets":["<Alpha>","<Beta>"]}}"""))
            .RootElement;
        var outer = JsonSerializer.SerializeToElement(new
        {
            type = "sub_session",
            tool_use_id = "toolu_1",
            agent = SampleIdentity.Alpha,
            @event = "message_start",
            data = inner
        });

        TurnStream.Apply(turn, SessionEvent.From("sub_session", outer));

        Assert.That(Delegation.From(turn.Blocks[0])!.Toolsets,
            Is.EqualTo(new[] { SampleToolset.Alpha, SampleToolset.Beta }));
    }

    /// <summary>Before the child's stream has started, the card has no toolsets to show.</summary>
    [Test]
    public void BeforeTheChildStarts_TheCardShowsNoToolsets() =>
        Assert.That(Delegation.From(Cook())!.Toolsets, Is.Empty);
}
