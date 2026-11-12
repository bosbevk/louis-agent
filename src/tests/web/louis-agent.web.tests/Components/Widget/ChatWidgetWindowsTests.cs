using System.Net;
using System.Text;
using LouisAgent.Web.Api;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Widget;
using LouisAgent.Web.Components.Widget.Door;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Widget;

/// <summary>
/// The guide's widget hands an ask to the business's window on the page (F127.4.2): when the guide asks a business a
/// window shows, the window gets the call, with the business's session once it starts, and hears the stream move on.
/// </summary>
public class ChatWidgetWindowsTests
{
    /// <summary>A guide turn that asks the shop, which answers in a session of its own.</summary>
    private const string AskStream = """
                                     event: message_start
                                     data: {"type":"message_start","message":{"session_id":"sess_fixture","role":"assistant"}}

                                     event: content_block_start
                                     data: {"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"toolu_ask","name":"AskAgent","input":{"address":"shop","message":"A room for Friday?"}}}

                                     event: sub_session
                                     data: {"type":"sub_session","tool_use_id":"toolu_ask","agent":"shop","event":"message_start","data":{"type":"message_start","message":{"session_id":"sess_shop","role":"assistant","toolsets":[]}}}

                                     event: sub_session
                                     data: {"type":"sub_session","tool_use_id":"toolu_ask","agent":"shop","event":"content_block_start","data":{"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}}

                                     event: sub_session
                                     data: {"type":"sub_session","tool_use_id":"toolu_ask","agent":"shop","event":"content_block_delta","data":{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"The garden room is free."}}}

                                     event: content_block_start
                                     data: {"type":"content_block_start","index":1,"content_block":{"type":"tool_result","tool_use_id":"toolu_ask","content":"The garden room is free.","is_error":false}}

                                     event: message_delta
                                     data: {"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"input_tokens":1,"output_tokens":1,"requests":1}}

                                     event: message_stop
                                     data: {"type":"message_stop"}

                                     """;

    /// <summary>
    /// The ask goes to the window that shows the shop, which knows the shop's session and its answer by the end, and
    /// hears the guide's stream move on.
    /// </summary>
    [Test]
    public void AnAskToABusinessOnThePage_GoesToItsWindow()
    {
        using var ctx = Context();
        var windows = new AgentWindows();
        ctx.Services.AddSingleton(windows);
        using var _ = windows.Show("shop");
        Block? heard = null;
        int moved = 0;
        windows.Asked += call => heard = call;
        windows.Moved += () => moved++;
        var cut = ctx.Render<ChatWidget>(parameters => parameters
            .Add(w => w.Identity, "guide").Add(w => w.Title, "Ask the guide"));
        cut.Find("button.widget-button").Click();

        cut.Find("#composer-input").Input("Show me the guesthouse live.");
        cut.Find("button.send").Click();
        cut.WaitForState(() => heard?.ToolResult is not null, TimeSpan.FromSeconds(10));

        Assert.Multiple(() =>
        {
            Assert.That(heard!.SubSessionId, Is.EqualTo("sess_shop"));
            Assert.That(heard.SubTurn!.Blocks.Single().Text, Is.EqualTo("The garden room is free."));
            Assert.That(moved, Is.GreaterThan(0));
        });
    }

    /// <summary>A context whose API answers a message with the guide's ask.</summary>
    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath.EndsWith("/messages")
            ? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(AskStream, Encoding.UTF8, "text/event-stream")
            }
            : FixtureApi.Answer(request));
        var api = new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, settings);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ChatText());
        return ctx;
    }
}
