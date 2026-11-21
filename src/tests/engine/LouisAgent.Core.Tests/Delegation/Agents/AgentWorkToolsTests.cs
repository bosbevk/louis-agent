using System.Net;
using System.Text;
using System.Text.Json;
using LouisAgent.Composition.Agents;
using LouisAgent.Core.Delegation.Agents;
using LouisAgent.Core.Sessions.SubSessions;

namespace LouisAgent.Core.Tests.Delegation.Agents;

/// <summary>
/// <c>ReadAgentWork</c> reads back what an agent asked in this chat did (F127.3.1 step 10): its steps as text, without
/// its thinking, the latest part when it is long, and only for an address this chat asked.
/// </summary>
public class AgentWorkToolsTests
{
    /// <summary>One event as the peer streamed it.</summary>
    private static SubSessionEvent Event(string type, string data) => new("call_1", "testprod", type, data);

    /// <summary>A text piece the agent said.</summary>
    private static SubSessionEvent Said(string text) =>
        Event("content_block_delta",
            $$$"""{"type":"content_block_delta","index":1,"delta":{"type":"text_delta","text":{{{JsonSerializer.Serialize(text)}}}}}""");

    /// <summary>A block the agent started.</summary>
    private static SubSessionEvent Block(string block) =>
        Event("content_block_start", $$"""{"type":"content_block_start","index":0,"content_block":{{block}}}""");

    /// <summary>A booking that failed, with the thinking that led to it.</summary>
    private static readonly SubSessionEvent[] Booking =
    [
        Event("message_start", """{"type":"message_start","message":{"session_id":"sess_1"}}"""),
        Block("""{"type":"thinking","thinking":""}"""),
        Event("content_block_delta",
            """{"type":"content_block_delta","index":0,"delta":{"type":"thinking_delta","thinking":"Secret plan."}}"""),
        Block("""{"type":"text","text":""}"""),
        Said("Garden room, R150 a dog."),
        Block("""{"type":"tool_use","id":"toolu_a","name":"SendLead","input":{"room":"garden"}}"""),
        Block("""{"type":"tool_result","tool_use_id":"toolu_a","content":"No email set.","is_error":true}""")
    ];

    /// <summary>
    /// The session's whole work as its server gives it (F127.4.2): the guide's ask, a turn the visitor had in the
    /// window, which the guide never streamed, and what the business did then.
    /// </summary>
    private const string ServerWork =
        """
        {"session_id":"sess_1","steps":[
          {"kind":"asked","text":"A room for Friday?"},
          {"kind":"said","text":"Garden room, R150 a dog."},
          {"kind":"asked","text":"Book it"},
          {"kind":"called","text":"{\"room\":\"garden\"}","tool":"SendLead"},
          {"kind":"returned","text":"Sent: the owner has it.","tool":"SendLead","failed":false}
        ]}
        """;

    /// <summary>A server that answers the work with this status and body.</summary>
    private static StubHandler Server(HttpStatusCode status, string body = "") =>
        new(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });

    /// <summary>
    /// The whole session is read from its server, so a turn the visitor typed in the window, which never streamed
    /// through this chat, is read too; the request carries the peer's key.
    /// </summary>
    [Test]
    public async Task TheWork_IsReadFromItsServer_WithTheTurnsTheVisitorHad()
    {
        var server = Server(HttpStatusCode.OK, ServerWork);

        string text = await ReadAsync("testprod", Booking, new HttpClient(server));

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("Asked: Book it"));
            Assert.That(text, Does.Contain("SendLead").And.Contain("Sent: the owner has it."));
            Assert.That(text, Does.Not.Contain("No email set."));
            Assert.That(server.Requests.Single().Url, Is.EqualTo("http://self.test/v1/sessions/sess_1/work"));
        });
    }

    /// <summary>A server that can't give the work leaves what streamed through this chat to read.</summary>
    [Test]
    public async Task WhenTheServerCantSay_WhatStreamedIsRead() =>
        Assert.That(await ReadAsync("testprod", Booking, new HttpClient(Server(HttpStatusCode.Conflict))),
            Does.Contain("Garden room, R150 a dog.").And.Contain("No email set."));

    /// <summary>Reads as the chat whose registry keeps one asked agent, testprod, with this work.</summary>
    private static async Task<string> ReadAsync(string address, IReadOnlyList<SubSessionEvent> events,
        HttpClient? http = null)
    {
        var chat = new SubSessionRegistry();
        await chat.AddAsync(new OpenSubSession(new Uri("http://self.test/"), "sess_1", "self", [],
            "testprod/voice-en", () => Task.CompletedTask)
        {
            Kind = OpenSubSession.AgentKind,
            Address = new AgentAddress("testprod", null),
            Work = new AgentWork(events)
        });
        using var _ = SubSessionRegistry.Use(chat);
        // No server to ask unless the test gives one: a 404, so what streamed is read.
        var tools = new AgentWorkTools("key", http ?? new HttpClient(Server(HttpStatusCode.NotFound)));
        return await tools.ReadAgentWork(address);
    }

    /// <summary>
    /// The work reads as its steps: what it said, the call with what it sent, and that the call failed.
    /// </summary>
    [Test]
    public async Task TheWork_ReadsAsItsSteps_WithoutTheThinking()
    {
        string text = await ReadAsync("testprod", Booking);

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("Garden room, R150 a dog."));
            Assert.That(text, Does.Contain("SendLead").And.Contain("""{"room":"garden"}"""));
            Assert.That(text, Does.Contain("failed").And.Contain("No email set."));
            Assert.That(text, Does.Not.Contain("Secret plan."));
        });
    }

    /// <summary>An address this chat never asked is refused, naming the ones it did.</summary>
    [Test]
    public async Task AnAddressNotAsked_IsRefused_NamingTheOnesAsked() =>
        Assert.That(await ReadAsync("testprod.specialist", Booking),
            Does.StartWith("Error").And.Contain("testprod.specialist").And.Contain("Asked: testprod"));

    /// <summary>Outside a chat nothing was kept, so there is nothing to read.</summary>
    [Test]
    public async Task OutsideAChat_ThereIsNothingToRead() =>
        Assert.That(await new AgentWorkTools().ReadAgentWork("testprod"), Does.StartWith("Error"));

    /// <summary>A long work keeps its latest part, within the limit, and says how much was cut.</summary>
    [Test]
    public async Task ALongWork_KeepsTheLatestPart_AndSaysTheCut()
    {
        var events = Enumerable.Range(0, 600).Select(n => Said($"Line {n:000} of a long answer. ")).ToList();

        string text = await ReadAsync("testprod", events);

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.StartWith("[The earliest"));
            Assert.That(text, Does.Contain("Line 599").And.Not.Contain("Line 000"));
            Assert.That(text, Has.Length.LessThanOrEqualTo(AgentWorkTools.MaxChars + 200));
        });
    }
}
