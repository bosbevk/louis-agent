using System.Reflection;
using LouisAgent.Testing;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Admin;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Pages.Admin;
using LouisAgent.Web.Scripts;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Pages.Admin;

/// <summary>
/// F144, Louis's pick, a home of cards: the admin's root is its home, each area the site named drawing its card there,
/// in the order named; a refused key, which a card reports, is asked for once, on the home (Louis's pick of three).
/// </summary>
public class AdminHomeTests
{
    /// <summary>A card a test can find by its class.</summary>
    private sealed class FirstCard : ComponentBase
    {
        /// <summary>One marked section.</summary>
        protected override void BuildRenderTree(RenderTreeBuilder builder) =>
            builder.AddMarkupContent(0, "<section class=\"first-card\">First</section>");
    }

    /// <summary>Another card.</summary>
    private sealed class SecondCard : ComponentBase
    {
        /// <summary>One marked section.</summary>
        protected override void BuildRenderTree(RenderTreeBuilder builder) =>
            builder.AddMarkupContent(0, "<section class=\"second-card\">Second</section>");
    }

    /// <summary>The home over these areas, with the admin's access the cards report to.</summary>
    private static (IRenderedComponent<AdminHome> Home, AdminAccess Access) Render(BunitContext ctx,
        params SiteArea[] areas)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var access = new AdminAccess();
        ctx.Services.AddSingleton(access);
        ctx.Services.AddSingleton(new SiteAreas(areas));
        var settings = new ChatSettings();
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(Handler) { BaseAddress = new Uri("http://localhost/") },
            settings));
        return (ctx.Render<AdminHome>(), access);
    }

    /// <summary>The API's answers: every session is ended.</summary>
    private static readonly StubHandler Handler = new(_ => new HttpResponseMessage(System.Net.HttpStatusCode.NoContent));

    /// <summary>
    /// F153: the home draws Reset my session above the cards, and a press resets this browser: its saved session
    /// ended, everything forgotten but the admin's key.
    /// </summary>
    [Test]
    public async Task TheHome_DrawsReset_AboveTheCards_AndAPressResets()
    {
        using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(FrameworkScripts.Reset);
        module.Setup<string[]>("chatKeys").SetResult(["louis-agent.widget-chats"]);
        var forget = module.SetupVoid("forget", ChatStore.AdminKeyKey);
        forget.SetVoidResult();
        ctx.JSInterop.Setup<string?>("localStorage.getItem", "louis-agent.widget-chats")
            .SetResult("""[{"sessionId":"guide-1","title":"t","turns":[]}]""");

        var (home, _) = Render(ctx, CardArea("overview", typeof(FirstCard)));
        var reset = home.Find(".admin-reset");
        string before = home.Find(".admin-reset button").TextContent.Trim();
        await home.Find(".admin-reset button").ClickAsync(new());

        // The page reloads at the end, so after the press the button still says it is resetting.
        Assert.Multiple(() =>
        {
            Assert.That(reset.NextElementSibling!.ClassName, Is.EqualTo("admin-home-cards"));
            Assert.That(before, Is.EqualTo("Reset my session"));
            Assert.That(home.Find(".admin-reset button").TextContent.Trim(), Is.EqualTo("Resetting…"));
            Assert.That(Handler.Requests.Select(r => r.Url), Does.Contain("http://localhost/v1/sessions/guide-1"));
            Assert.That(forget.Invocations, Has.Count.EqualTo(1));
        });
    }

    /// <summary>An area that brings this card.</summary>
    private static SiteArea CardArea(string name, Type card) => new(name, _ => { }, [], AdminCard: card);

    /// <summary>The home is the admin's root.</summary>
    [Test]
    public void TheHome_IsTheAdminsRoot() =>
        Assert.That(typeof(AdminHome).GetCustomAttribute<RouteAttribute>()!.Template, Is.EqualTo("/admin"));

    /// <summary>Each named area's card, in the order the site named them.</summary>
    [Test]
    public void TheHome_DrawsEachAreasCard_InTheOrderNamed()
    {
        using var ctx = new BunitContext();

        var (home, _) = Render(ctx, CardArea("evals", typeof(SecondCard)), CardArea("overview", typeof(FirstCard)));

        Assert.That(home.FindAll(".admin-home-cards > section").Select(card => card.ClassName),
            Is.EqualTo(new[] { "second-card", "first-card" }));
    }

    /// <summary>With no area to plug in, the home says so.</summary>
    [Test]
    public void WithNoArea_TheHomeSaysSo()
    {
        using var ctx = new BunitContext();

        var (home, _) = Render(ctx);

        Assert.That(home.Find(".admin-home-none").TextContent, Does.Contain("No area"));
    }

    /// <summary>A card's refused key shows the box to enter one, once, and a saved key takes it away.</summary>
    [Test]
    public void ARefusedKey_IsAskedForOnce_AndASavedKeyTakesTheBoxAway()
    {
        using var ctx = new BunitContext();
        var (home, access) = Render(ctx, CardArea("overview", typeof(FirstCard)));

        home.InvokeAsync(() => access.Refuse("The admin key is wrong."));
        int boxes = home.FindAll("input[type=password]").Count;
        home.Find("input[type=password]").Change("the-key");
        home.Find("form.admin-key").Submit();

        home.WaitForAssertion(() => Assert.That(home.FindAll("input[type=password]"), Is.Empty),
            TimeSpan.FromSeconds(10));
        Assert.That(boxes, Is.EqualTo(1));
    }
}
