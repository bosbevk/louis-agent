using LouisAgent.Core.Sessions.SubSessions;

namespace LouisAgent.Core.Tests.Sessions.SubSessions;

/// <summary>
/// An asked agent's work is the events it streamed (F127.3.1 step 9): its tool calls are read from them in order, each
/// marked when it failed, and its thinking, its text and a deeper child's events are not its calls.
/// </summary>
public class AgentWorkTests
{
    /// <summary>One event as the peer streamed it.</summary>
    private static SubSessionEvent Event(string type, string data) => new("call_1", "testprod", type, data);

    /// <summary>A block the agent started: thinking, text, a tool call or a tool's result.</summary>
    private static SubSessionEvent Block(string block) =>
        Event("content_block_start", $$"""{"type":"content_block_start","index":0,"content_block":{{block}}}""");

    /// <summary>Thinking and text, the way every answer streams them.</summary>
    private static readonly SubSessionEvent[] ThinkingAndText =
    [
        Block("""{"type":"thinking","thinking":""}"""),
        Event("content_block_delta",
            """{"type":"content_block_delta","index":0,"delta":{"type":"thinking_delta","thinking":"Call SendLead?"}}"""),
        Block("""{"type":"text","text":""}"""),
        Event("content_block_delta",
            """{"type":"content_block_delta","index":1,"delta":{"type":"text_delta","text":"Garden room."}}""")
    ];

    /// <summary>An answer that called no tool has no calls, whatever it thought or wrote.</summary>
    [Test]
    public void ThinkingAndText_AreNoCalls() =>
        Assert.That(new AgentWork(ThinkingAndText).Calls(), Is.Empty);

    /// <summary>The calls come in the order made, and a result marked as an error marks its own call.</summary>
    [Test]
    public void TheCalls_ComeInOrder_EachMarkedWhenItFailed()
    {
        var work = new AgentWork(
        [
            .. ThinkingAndText,
            Block("""{"type":"tool_use","id":"toolu_a","name":"SendLead","input":{}}"""),
            Block("""{"type":"tool_use","id":"toolu_b","name":"ShowPlace","input":{"place":"garden"}}"""),
            Block("""{"type":"tool_result","tool_use_id":"toolu_b","content":"No such place.","is_error":true}"""),
            Block("""{"type":"tool_result","tool_use_id":"toolu_a","content":"Sent.","is_error":false}""")
        ]);

        Assert.That(work.Calls(),
            Is.EqualTo(new[] { new AgentWork.ToolCall("SendLead", false), new AgentWork.ToolCall("ShowPlace", true) }));
    }

    /// <summary>
    /// A deeper child's events, relayed inside the agent's stream, are that child's calls, not the agent's.
    /// </summary>
    [Test]
    public void ADeeperChildsCalls_AreNotTheAgents()
    {
        var relayed = Event("sub_session",
            """{"type":"sub_session","tool_use_id":"toolu_x","agent":"cook","event":"content_block_start","data":{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"toolu_y","name":"FetchUrl","input":{}}}}""");

        Assert.That(new AgentWork([relayed]).Calls(), Is.Empty);
    }

    /// <summary>A follow-up's events come after the earlier ones, and the earlier work is left as it was.</summary>
    [Test]
    public void Then_PutsTheLaterEventsAfter()
    {
        var first = new AgentWork([Block("""{"type":"tool_use","id":"toolu_a","name":"SendLead","input":{}}""")]);

        var both = first.Then([Block("""{"type":"tool_use","id":"toolu_b","name":"ShowPlace","input":{}}""")]);

        Assert.Multiple(() =>
        {
            Assert.That(both.Calls().Select(call => call.Name), Is.EqualTo(new[] { "SendLead", "ShowPlace" }));
            Assert.That(first.Events, Has.Count.EqualTo(1));
        });
    }
}
