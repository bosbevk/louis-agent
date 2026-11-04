using System.Net;
using System.Text;
using LouisAgent.Testing;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Chats;
using LouisAgent.Web.Shared.Storage;
using Microsoft.Extensions.DependencyInjection;
using PromptsPage = LouisAgent.Puls.Pages.Admin.Prompts;

namespace LouisAgent.Puls.Tests.Pages.Admin;

/// <summary>
/// CL27: the Prompts page lists every built-in prompt as its header says it: its name and file, its level, what it
/// is, and the toolsets it always loads.
/// </summary>
public class PromptsPageTests
{
    /// <summary>A mode, a role that always loads a toolset, and the principles, which name no level.</summary>
    private const string List = """
        {"prompts":[
          {"path":"agent/acme/voice.md","name":"voice","description":"Acme's voice.","level":"agent","toolsets":[]},
          {"path":"shared/principles.md","name":"principles","description":"What holds for every agent.","level":null,"toolsets":[]},
          {"path":"specialists/acme-cook.md","name":"acme-cook","description":"Acme's cook.","level":"specialist","toolsets":["shops?"]}
        ]}
        """;

    /// <summary>Each prompt is a row with its name, file, level, description and toolsets.</summary>
    [Test]
    public void EveryPrompt_IsARow_AsItsHeaderSays()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx);

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".prompt-row"), Has.Count.EqualTo(3)),
            TimeSpan.FromSeconds(10));
        var cook = cut.FindAll(".prompt-row")[2];
        Assert.That(cook.TextContent, Does.Contain("acme-cook").And.Contain("specialists/acme-cook.md")
            .And.Contain("specialist").And.Contain("Acme's cook.").And.Contain("shops?"));
        Assert.That(cut.FindAll(".prompt-row")[1].QuerySelector(".prompt-level")!.TextContent.Trim(),
            Is.EqualTo("—"), "the principles name no level");
    }

    /// <summary>The page over an API that serves the list.</summary>
    private static IRenderedComponent<PromptsPage> Render(BunitContext ctx)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath.EndsWith("/v1/admin/prompts")
            ? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(List, Encoding.UTF8, "application/json")
            }
            : new HttpResponseMessage(HttpStatusCode.NotFound));

        var settings = new ChatSettings();
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") },
            settings));
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        return ctx.Render<PromptsPage>();
    }
}
