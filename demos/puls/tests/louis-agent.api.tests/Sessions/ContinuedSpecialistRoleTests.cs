using LouisAgent.Testing;
using ClaudeStreamFormat = LouisAgent.Api.Sessions.Streaming.Claude.ClaudeStreamFormat;
using System.Text.Json;
using LouisAgent.SessionStream;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Text;
using LouisAgent.Web.Api;

namespace LouisAgent.Api.Tests.Sessions;

/// <summary>
/// An ask names an address, not a role or toolsets, so a follow-up's card and its work line learn them from the start
/// of the child's own stream: each session's message_start says its role (session_role: "role" is already Claude's
/// speaker) and what it loaded.
/// </summary>
public class ContinuedSpecialistRoleTests
{
    /// <summary>A session's stream starts by saying its role.</summary>
    [Test]
    public void ASessionsStream_StartsWithItsRole()
    {
        var start =
            new ClaudeStreamFormat("sess_1", [SampleToolset.Alpha, SampleToolset.Beta], SampleRole.Alpha).Start();

        using var data = JsonDocument.Parse(start.Data);
        var message = data.RootElement.GetProperty("message");
        Assert.That(message.GetProperty("session_role").GetString(), Is.EqualTo(SampleRole.Alpha));
        Assert.That(message.GetProperty("role").GetString(), Is.EqualTo("assistant"), "Claude's speaker, unchanged");
    }

    /// <summary>A turn with a follow-up that has started and not finished.</summary>
    private static Turn AFollowUpThatStarted()
    {
        var turn = new Turn
        {
            Blocks =
            [
                new Block
                {
                    Kind = BlockKind.Tool,
                    ToolName = Delegation.AskToolName,
                    ToolUseId = "toolu_9",
                    ToolInput = """{"address":"acme.cook","message":"**Wil hê:** vir ses mense"}""",
                },
            ],
        };
        var inner = JsonDocument
            .Parse(SampleToolset.Fill(SampleRole.Fill(
                """{"type":"message_start","message":{"session_id":"sess_2","role":"assistant","toolsets":["<Alpha>","<Beta>"],"session_role":"<RoleAlpha>"}}""")))
            .RootElement;
        var outer = JsonSerializer.SerializeToElement(new
        {
            type = "sub_session",
            tool_use_id = "toolu_9",
            agent = SampleIdentity.Alpha,
            @event = "message_start",
            data = inner
        });
        TurnStream.Apply(turn, SessionEvent.From("sub_session", outer));
        return turn;
    }

    /// <summary>A follow-up's card names what the child loaded while it works.</summary>
    [Test]
    public void AFollowUpsCard_NamesWhatItLoaded_WhileItWorks() =>
        Assert.That(Delegation.From(AFollowUpThatStarted().Blocks[0])!.Toolsets,
            Is.EqualTo(new[] { SampleToolset.Alpha, SampleToolset.Beta }));

    /// <summary>A follow-up's line says what the cook is doing.</summary>
    [Test]
    public void AFollowUpsLine_SaysWhatTheCookIsDoing()
    {
        var text = new ChatText
        {
            Processing = "BESIG",
            RolePhases = new Dictionary<string, RolePhases>
            {
                [SampleRole.Alpha] = new("Soek bestanddele", "Bou resep")
            }
        };

        Assert.That(WorkStatus.Label(AFollowUpThatStarted(), text), Is.EqualTo("Soek bestanddele"));
    }
}
