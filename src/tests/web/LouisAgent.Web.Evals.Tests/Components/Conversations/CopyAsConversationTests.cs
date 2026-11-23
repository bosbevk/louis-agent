using System.Net;
using LouisAgent.Web.Api;
using LouisAgent.Web.Evals.Client;
using LouisAgent.Web.Evals.Components.Conversations;
using LouisAgent.Web.Scripts;
using LouisAgent.Web.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Evals.Tests.Components.Conversations;

/// <summary>
/// F126.4: Copy as conversation reads the conversation as a block for the eval file and copies it with the framework's
/// own copy to the clipboard, then says so; when the browser didn't allow it, it says that plainly.
/// </summary>
public class CopyAsConversationTests
{
    private const string Block = """{"id": "guide-walk", "turns": []}""";

    /// <summary>The button copies the block the API answered, and says it was copied.</summary>
    [Test]
    public void TheButton_CopiesTheBlock_AndSaysSo()
    {
        using var ctx = new BunitContext();
        var copy = ctx.JSInterop.SetupModule(FrameworkScripts.CodeListing).Setup<bool>("copy", Block);
        copy.SetResult(true);
        var (button, stub) = Render(ctx);

        button.Find("button.conversation-copy").Click();

        button.WaitForAssertion(() =>
            Assert.That(button.Find("button.conversation-copy").TextContent, Is.EqualTo("Copied ✓")));
        Assert.That(button.Find(".conversation-copied").TextContent, Does.Contain("guide.eval.json"));
        Assert.That(stub.Requests.Single().Url,
            Is.EqualTo("http://localhost/v1/admin/evals/runs/20261123-131900-guide/guide-walk/as-file"));
    }

    /// <summary>When the browser didn't allow the copy, the page says so plainly.</summary>
    [Test]
    public void ACopyTheBrowserRefused_IsSaidPlainly()
    {
        using var ctx = new BunitContext();
        var copy = ctx.JSInterop.SetupModule(FrameworkScripts.CodeListing).Setup<bool>("copy", Block);
        copy.SetResult(false);
        var (button, _) = Render(ctx);

        button.Find("button.conversation-copy").Click();

        button.WaitForAssertion(() =>
            Assert.That(button.Find(".conversation-copied").TextContent, Does.Contain("didn't allow")));
    }

    /// <summary>The button over a stub of the API that answers the block.</summary>
    private static (IRenderedComponent<CopyAsConversation> Button, StubHandler Stub) Render(BunitContext ctx)
    {
        var stub = new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Block) });
        var api = new AgentApi(new HttpClient(stub) { BaseAddress = new Uri("http://localhost/") }, new ChatSettings());
        ctx.Services.AddSingleton(new EvalsApi(api));
        var button = ctx.Render<CopyAsConversation>(parameters => parameters.Add(p => p.Eval, "guide")
            .Add(p => p.Run, "20261123-131900-guide").Add(p => p.Conversation, "guide-walk"));
        return (button, stub);
    }
}
