using System.Text.Json;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Components.Admin;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Components.Admin;

// F18 step 5c.3: a category's line links to its tasks (`?task=`), and the run explorer opens that task's conversation at once. Written first: the explorer opened at the run.
public class RunExplorerOpenTaskTests
{
    private static string Run(int repeats) => $$"""
        {"run":{"id":"run123456789","check":"puls-cook","repeat":{{repeats}}},"modes":[{{string.Join(",", Enumerable.Range(1, repeats).Select(r => $$$"""{"name":"puls-cook","repeat":{{{r}}},"toolsets":["web"]}"""))}}],
         "conversations":[{{string.Join(",", Enumerable.Range(1, repeats).SelectMany(r => new[] { "cook-af", "cook-en" }.Select(t => $$$"""{"mode":"puls-cook","repeat":{{{r}}},"task":"{{{t}}}","role":null,"scores":{},"errors":0}""")))}}],
         "turns":[],"requests":[]}
        """;

    private static IRenderedComponent<RunExplorer> Render(string? openTask, int repeats = 1)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(new StubHandler(_ => new HttpResponseMessage())) { BaseAddress = new Uri("http://localhost/") }, new ChatSettings()));
        ctx.Services.AddSingleton(new ReviewerState(ctx.JSInterop.JSRuntime));
        return ctx.Render<RunExplorer>(p => p.Add(x => x.Run, JsonDocument.Parse(Run(repeats)).RootElement.Clone()).Add(x => x.OpenTask, openTask));
    }

    [Test]
    public void ATaskToOpen_OpensItsConversation_AtOnce() =>
        Assert.That(Render("cook-en").Find(".crumb.here").TextContent, Is.EqualTo("cook-en"));

    // Louis, 2026-10-26 ("this page changes when clicking there", the repeat buttons): a change of repeat kept the page at the run overview whatever was open. It keeps the conversation
    // you are in when the other repeat has it too, and falls back to the overview only when it doesn't.
    [Test]
    public void ChangingTheRepeat_KeepsTheOpenConversation()
    {
        var cut = Render("cook-en", repeats: 2);

        cut.FindAll(".segmented button").First(b => b.TextContent == "2").Click();

        Assert.That(cut.Find(".crumb.here").TextContent, Is.EqualTo("cook-en"));
        Assert.That(cut.Find(".segmented button.active").TextContent, Is.EqualTo("2"));
    }

    [Test]
    public void NoTaskToOpen_StaysAtTheRun() =>
        Assert.That(Render(null).Find(".crumb.here").TextContent, Is.EqualTo("Run"));
}
