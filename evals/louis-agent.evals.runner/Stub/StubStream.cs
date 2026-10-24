using LouisAgent.Core.Messages;
using LouisAgent.Evals.Runner.Messages;

namespace LouisAgent.Evals.Runner.Stub;

/// <summary>
/// The events of one canned turn, as the API streams them: the Python stub's <c>stream()</c>, with the same events, the same
/// order and the same keys. A turn with the cook has a StartSubSession call and the cook's events nested as sub_session.
/// </summary>
public static class StubStream
{
    private const string CookReply = "Die kok se resep.";

    public static IReadOnlyList<(string Type, object Data)> Turn(string task, int turn, bool cook)
    {
        var events = new List<(string Type, object Data)>
        {
            ("message_start", new { type = "message_start", message = new { id = $"msg_{task}_{turn}" } }),
        };
        int index = 0;

        void Block(object contentBlock, params object[] deltas)
        {
            events.Add(("content_block_start", new { type = "content_block_start", index, content_block = contentBlock }));
            foreach (var delta in deltas)
                events.Add(("content_block_delta", new { type = "content_block_delta", index, delta }));
            events.Add(("content_block_stop", new { type = "content_block_stop", index }));
            index++;
        }

        Block(new { type = "thinking", thinking = "" },
            new { type = "thinking_delta", thinking = MessageText.Format(StubMessage.ThinkingFirst) },
            new { type = "thinking_delta", thinking = MessageText.Format(StubMessage.ThinkingSecond) });

        if (cook)
        {
            string call = $"toolu_{task}_{turn}";
            Block(new { type = "tool_use", id = call, name = "StartSubSession", input = new { agent = "puls", role = "puls-cook", task } });
            var inner = new (string Event, object Data)[]
            {
                ("content_block_start", new { type = "content_block_start", index = 0, content_block = new { type = "text", text = "" } }),
                ("content_block_delta", new { type = "content_block_delta", index = 0, delta = new { type = "text_delta", text = CookReply } }),
                ("content_block_stop", new { type = "content_block_stop", index = 0 }),
            };
            foreach (var (name, data) in inner)
                events.Add(("sub_session", new { tool_use_id = call, agent = "puls", @event = name, data }));
            Block(new { type = "tool_result", tool_use_id = call, content = CookReply, is_error = false });
        }

        Block(new { type = "text", text = "" },
            new { type = "text_delta", text = MessageText.Format(StubMessage.AnswerStart, new { Task = task }) },
            new { type = "text_delta", text = MessageText.Format(StubMessage.AnswerEnd) });
        events.Add(("message_delta", new { type = "message_delta", delta = new { stop_reason = "end_turn" }, usage = (object?)null }));
        events.Add(("message_stop", new { type = "message_stop" }));
        return events;
    }
}
