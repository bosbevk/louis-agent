using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Chat;
using LouisAgent.Web.Scripts;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;
using ChatModel = LouisAgent.Web.Chats.Chat;

namespace LouisAgent.Web.Tests.Components.Chat;

/// <summary>
/// The message list follows the answer to its end (FX98, Louis: "after a chat turn the chat does not scroll fully to
/// the bottom"): it scrolls while the turn streams and once more on the draw after the stream ends, the one that adds
/// the cost line and takes the caret away, so the last line is in view.
/// </summary>
public class MessageListTests
{
    /// <summary>A chat with one answered turn.</summary>
    private static ChatModel OneTurn() => new()
    {
        SessionId = "sess_test",
        Turns = [new Turn { UserText = "hi", Blocks = [new Block { Kind = BlockKind.Text, Text = "Hello." }] }],
    };

    /// <summary>The draw after the stream ends scrolls once more, so the cost line that draw adds is in view.</summary>
    [Test]
    public void TheDrawAfterTheStreamEnds_ScrollsOnceMore()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ChatText());
        var module = ctx.JSInterop.SetupModule(FrameworkScripts.MessageList);
        module.SetupVoid("scrollToBottom", _ => true).SetVoidResult();
        var chat = OneTurn();

        var cut = ctx.Render<MessageList>(parameters => parameters.Add(list => list.Chat, chat).Add(list => list.Streaming, true));
        int whileStreaming = module.Invocations.Count(call => call.Identifier == "scrollToBottom");
        cut.Render(parameters => parameters.Add(list => list.Chat, chat).Add(list => list.Streaming, false));

        Assert.That(module.Invocations.Count(call => call.Identifier == "scrollToBottom"), Is.EqualTo(whileStreaming + 1), "one more scroll for the closing draw");
    }

    /// <summary>A draw while nothing streams and nothing changed scrolls not at all, so a reader who scrolled up stays put.</summary>
    [Test]
    public void AQuietDraw_DoesNotScroll()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ChatText());
        var module = ctx.JSInterop.SetupModule(FrameworkScripts.MessageList);
        module.SetupVoid("scrollToBottom", _ => true).SetVoidResult();
        var chat = OneTurn();

        var cut = ctx.Render<MessageList>(parameters => parameters.Add(list => list.Chat, chat).Add(list => list.Streaming, false));
        int afterFirst = module.Invocations.Count(call => call.Identifier == "scrollToBottom");
        cut.Render(parameters => parameters.Add(list => list.Chat, chat).Add(list => list.Streaming, false));

        Assert.That(module.Invocations.Count(call => call.Identifier == "scrollToBottom"), Is.EqualTo(afterFirst));
    }
}
