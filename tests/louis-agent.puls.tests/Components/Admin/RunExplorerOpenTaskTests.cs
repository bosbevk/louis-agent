using System.Text.Json;
using LouisAgent.Puls.Components.Admin;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Components.Admin;

// F18 step 5c.3: a category's line links to its tasks (`?task=`), and the run explorer opens that task's conversation at once. Written first: the explorer opened at the run.
public class RunExplorerOpenTaskTests
{
    private const string Run = """
        {"run":{"id":"run123456789","check":"puls-cook","repeat":1},"modes":[{"name":"puls-cook","repeat":1,"toolsets":["web"]}],
         "conversations":[{"mode":"puls-cook","repeat":1,"task":"cook-af","role":null,"scores":{},"errors":0},{"mode":"puls-cook","repeat":1,"task":"cook-en","role":null,"scores":{},"errors":0}],
         "turns":[],"requests":[]}
        """;

    private static IRenderedComponent<RunExplorer> Render(string? openTask)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(new StubHandler(_ => new HttpResponseMessage())) { BaseAddress = new Uri("http://localhost/") }, new ChatSettings()));
        return ctx.Render<RunExplorer>(p => p.Add(x => x.Run, JsonDocument.Parse(Run).RootElement.Clone()).Add(x => x.OpenTask, openTask));
    }

    [Test]
    public void ATaskToOpen_OpensItsConversation_AtOnce() =>
        Assert.That(Render("cook-en").Find(".crumb.here").TextContent, Is.EqualTo("cook-en"));

    [Test]
    public void NoTaskToOpen_StaysAtTheRun() =>
        Assert.That(Render(null).Find(".crumb.here").TextContent, Is.EqualTo("Run"));
}
