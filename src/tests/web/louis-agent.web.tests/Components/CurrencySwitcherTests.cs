using System.Net;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components;

/// <summary>
/// The currency switcher: the choices the API's rates give, the server's region first, pinned to a golden file; a
/// choice converts every cost from then on. The API is a stub answering the rates; the saved choice comes from JS,
/// which the loose interop answers with nothing.
/// </summary>
public class CurrencySwitcherTests
{
    /// <summary>The currency state over the stub API.</summary>
    private static CurrencyState State(BunitContext ctx)
    {
        var http = new HttpClient(new StubHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/v1/rates" => Json("""{"as_of":"2026-10-13","base":"USD","rates":{"ZAR":18.0,"EUR":0.9}}"""),
            "/v1/rates/display" => Json("""{"display_currency":"ZAR","display_symbol":"R","base":"USD","symbols":{"USD":"$","ZAR":"R","EUR":"€"}}"""),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        }))
        { BaseAddress = new Uri("http://localhost/") };
        return new CurrencyState(new AgentApi(http, new ChatSettings()), ctx.JSInterop.JSRuntime);
    }

    /// <summary>A 200 with this body as JSON.</summary>
    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

    /// <summary>The choices match the golden file.</summary>
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
        Snapshots.Match("Components/CurrencySwitcher.html", cut.Markup);
    }

    /// <summary>Choosing a currency converts from then on, and is kept in this browser.</summary>
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
