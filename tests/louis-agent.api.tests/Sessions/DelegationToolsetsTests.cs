using LouisAgent.Testing;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Chats;
using ClaudeStreamFormat = LouisAgent.Api.Sessions.Streaming.Claude.ClaudeStreamFormat;
using System.Text.Json;
using LouisAgent.SessionStream;
using LouisAgent.Web.Shared;

namespace LouisAgent.Api.Tests.Sessions;

/// <summary>
/// A delegation card shows the toolsets the specialist actually loaded, not just the ones Puls sent (F34). From
/// Louis's screenshot: the cook's chips said `web`, though its role also adds `shops` on the peer. Each session's
/// message_start says what it loaded, and the card shows that.
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

    /// <summary>The cook's call as a block, with its answer and footer once it returned.</summary>
    private static Block Cook(string? footer = null) => new()
    {
        Kind = BlockKind.Tool,
        ToolName = Delegation.ToolName,
        ToolUseId = "toolu_1",
        ToolInput = SampleToolset.Fill(
            SampleRole.Fill("""{"toolsets":["<Alpha>"],"message":"**Vir:** kok","role":"<RoleAlpha>"}""")),
        ToolResult = footer is null ? null : "Die resep.\n\n" + footer,
    };

    /// <summary>
    /// While the specialist works, its own stream's start says what it loaded, and the card shows that.
    /// </summary>
    [Test]
    public void TheCard_ShowsWhatTheSpecialistLoaded_WhileItWorks()
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

    /// <summary>Once it's done, the card takes the toolsets from the footer.</summary>
    [Test]
    public void TheCard_ShowsTheFootersToolsets_OnceItsDone() =>
        Assert.That(
            Delegation.From(Cook(SampleToolset.Fill(SampleRole.Fill(
                    "[Sub-session sub-1 on puls (<Alpha>, <Beta>; <RoleAlpha>). It stays open: …]"))))!
                .Toolsets,
            Is.EqualTo(new[] { SampleToolset.Alpha, SampleToolset.Beta }));

    /// <summary>A chat with neither a live stream nor a footer shows the toolsets that were sent.</summary>
    [Test]
    public void AnOlderChat_ShowsTheToolsetsSent() =>
        Assert.That(Delegation.From(Cook())!.Toolsets, Is.EqualTo(new[] { SampleToolset.Alpha }));
}
