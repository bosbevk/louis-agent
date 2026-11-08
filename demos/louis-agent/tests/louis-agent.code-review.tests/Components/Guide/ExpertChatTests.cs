using LouisAgent.Choices;
using LouisAgent.CodeReview.Components.Guide;
using LouisAgent.Identities.Expert;
using LouisAgent.Web.Api;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.CodeReview.Tests.Components.Guide;

/// <summary>
/// The guide in the corner (F109.3): the framework's widget wrapped once with the expert identity and the site's
/// words, so a page places one tag and a question opens a session as the expert.
/// </summary>
public class ExpertChatTests
{
    /// <summary>A context with what the widget asks for, over the stub API, which remembers what was sent.</summary>
    private static BunitContext Context(out StubHandler handler)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var api = FixtureApi.Client(settings, out handler);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ChatText());
        return ctx;
    }

    /// <summary>Opened, it carries the site's words; a question opens a session as the expert.</summary>
    [Test]
    public void TheGuide_SpeaksTheSitesWords_AndOpensAsTheExpert()
    {
        using var ctx = Context(out var handler);
        var cut = ctx.Render<ExpertChat>();

        cut.Find("button.widget-button").Click();
        cut.Find("#composer-input").Input("What is a pack?");
        cut.Find("button.send").Click();
        cut.WaitForState(() => cut.FindAll(".turn").Count > 0, TimeSpan.FromSeconds(10));
        var opened = handler.Requests.Single(r => r.Method == HttpMethod.Post && r.Url.EndsWith("/v1/sessions"));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".widget-title").TextContent, Is.EqualTo("Ask the guide"));
            Assert.That(opened.Body, Does.Contain($"\"identity\":\"{ExpertIdentityName.LouisAgentExpert.Wire()}\""));
        });
    }
}
