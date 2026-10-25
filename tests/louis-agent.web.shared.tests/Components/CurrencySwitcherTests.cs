using System.Net;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Components;
using LouisAgent.Web.Shared.Storage;
using LouisAgent.Web.Shared.Text;
using LouisAgent.Web.Shared.Usage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Shared.Tests.Components;

// T1: the currency switcher (F14): the choices the API's rates give, the server's region first, pinned to
// CurrencySwitcher.html; a choice converts every cost from then on. The API is a stub answering /rates and
// /rates/display; the saved choice comes from JS (localStorage), which the loose interop answers with nothing.
public class CurrencySwitcherTests
{
    private static CurrencyState State(BunitContext ctx)
    {
        var http = new HttpClient(new StubHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/v1/rates" => Json("""{"as_of":"2026-10-13","base":"USD","rates":{"ZAR":18.0,"EUR":0.9}}"""),
            "/v1/rates/display" => Json("""{"display_currency":"ZAR","display_symbol":"R","base":"USD","symbols":{"USD":"$","ZAR":"R","EUR":"€"}}"""),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        })) { BaseAddress = new Uri("http://localhost/") };
        return new CurrencyState(new AgentApi(http, new ChatSettings()), ctx.JSInterop.JSRuntime);
    }

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

    [Test]
    public void TheChoices_MatchTheGoldenFile()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var state = State(ctx);
        ctx.Services.AddSingleton(state);
        ctx.Services.AddSingleton(new ChatText());

        var cut = ctx.Render<CurrencySwitcher>();
        cut.WaitForState(() => cut.FindAll("option").Count > 0);

        Assert.That(state.Current!.Code, Is.EqualTo("ZAR"), "the server's region, until the user chooses");
        Assert.That(cut.FindAll("option").Select(o => o.GetAttribute("value")), Is.EqualTo(new[] { "USD", "ZAR", "EUR" }), "the charged one, the server's, then the rest");
        Snapshots.Match("louis-agent.web.shared.tests", "Components/CurrencySwitcher.html", cut.Markup);
    }

    [Test]
    public void ChoosingACurrency_ConvertsFromThenOn_AndIsKeptInThisBrowser()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var state = State(ctx);
        ctx.Services.AddSingleton(state);
        ctx.Services.AddSingleton(new ChatText());
        var cut = ctx.Render<CurrencySwitcher>();
        cut.WaitForState(() => cut.FindAll("option").Count > 0);

        cut.Find("select").Change("EUR");

        cut.WaitForState(() => state.Current!.Code == "EUR");
        Assert.That(state.Current!.Converts, Is.True);
        Assert.That(state.Current.Rate, Is.EqualTo(0.9m));
        Assert.That(UsageFormat.Money(1m, "USD", state.Current), Is.EqualTo("€0.9"), "two significant digits below 1, as the ledger writes it");
        Assert.That(ctx.JSInterop.Invocations.Select(i => i.Identifier), Does.Contain("localStorage.setItem"));
    }
}
