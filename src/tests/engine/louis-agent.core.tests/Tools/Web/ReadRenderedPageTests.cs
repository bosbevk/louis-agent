using LouisAgent.Core.Pricing.Rates;
using System.Net;
using System.Text;
using System.Text.Json;
using LouisAgent.Core.Config;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Agent;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Tools.Web;
using LouisAgent.Core.Tools.Web.Fetching;
using LouisAgent.Core.Tools.Web.Search;
using LouisAgent.Core.Tools.Web.Rendering;
using LouisAgent.Core.Tools.Web.Rendering.Providers;
using LouisAgent.Core.Tools.Web.Search.Providers;

using static LouisAgent.Core.Tests.Tools.Web.FakeWeb;

namespace LouisAgent.Core.Tests.Tools.Web;

// ReadRenderedPage (F25 step 7, Louis: "use tavily extract, but call the tool something else, keep the web search"): a page a browser has to draw, read through the renderer, framed as untrusted.
public class ReadRenderedPageTests
{
    // --- ReadRenderedPage (F25 step 7, Louis: "use tavily extract, but call the tool something else, keep the web search") ---
    [Test]
    public async Task ReadRenderedPage_PostsTheUrlToTavilyExtract_AndFramesTheTextAsUntrusted()
    {
        // A Pick n Pay product page draws its price with JavaScript: FetchUrl gets "(no readable text)"; Tavily's
        // Extract renders it. The key goes in the header, never in the body; the page is framed like a fetched one.
        HttpRequestMessage? sent = null;
        string? body = null;
        var renderer = new TavilyPageRenderer("tvly-key", Http(r => { sent = r; body = r.Content!.ReadAsStringAsync().Result; return Html(ExtractJson, "application/json"); }));

        string result = await WebToolsOn(_ => Html(""), renderer: renderer).ReadRenderedPage("https://www.pnp.co.za/pnp-large-eggs-6-pack/p/118676_EA");

        Assert.That(sent!.Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(sent.RequestUri!.ToString(), Is.EqualTo("https://api.tavily.com/extract"));
        Assert.That(sent.Headers.Authorization!.ToString(), Is.EqualTo("Bearer tvly-key"));
        using var json = JsonDocument.Parse(body!);
        Assert.That(json.RootElement.GetProperty("urls")[0].GetString(), Is.EqualTo("https://www.pnp.co.za/pnp-large-eggs-6-pack/p/118676_EA"));
        Assert.That(json.RootElement.GetProperty("extract_depth").GetString(), Is.EqualTo("advanced"), "the JavaScript sites are why the tool exists");
        Assert.That(body, Does.Not.Contain("tvly-key"));

        Assert.That(result, Does.StartWith("URL: https://www.pnp.co.za/pnp-large-eggs-6-pack/p/118676_EA"));
        Assert.That(result, Does.Contain("untrusted web content").And.Contain("R34.99"));
    }

    [Test]
    public async Task ReadRenderedPage_ReportsAPageTavilyCouldNotRender()
    {
        // Tavily answers 200 with the URL under failed_results: an answer the model can act on, not an empty page.
        var renderer = new TavilyPageRenderer("k", Http(_ => Html("""{"results":[],"failed_results":[{"url":"https://example.com/x","error":"Failed to fetch URL"}]}""", "application/json")));

        string result = await WebToolsOn(_ => Html(""), renderer: renderer).ReadRenderedPage("https://example.com/x");

        Assert.That(result, Does.StartWith("Error").And.Contain("https://example.com/x").And.Contain("Failed to fetch URL"));
    }

    [Test]
    public async Task ReadRenderedPage_WithoutAKey_SaysWhatToSet_WithoutANetworkCall()
    {
        var handler = new StubHandler(_ => Html(""));
        var tools = new WebTools(new FakeSearch(), new PageFetcher(handler, PublicDns), PageRendererFactory.Create(new AgentOptions()));

        string result = await tools.ReadRenderedPage("https://example.com/x");

        // F32 step 3: either renderer will do, the headless Chrome we run or Tavily's Extract.
        Assert.That(result, Does.StartWith("Error").And.Contain("BROWSER_URL").And.Contain("TAVILY_API_KEY"));
        Assert.That(handler.Requests, Is.Empty);
    }

    [Test]
    public void ReadRenderedPage_SaysNoMoreThanIsTrueOfEveryRenderer()
    {
        // F32 step 3: the description named Tavily and its credits; the renderer is configuration now (Chrome, which
        // costs nothing, or Tavily), so it says what's true of both: slower than FetchUrl, and the address rule.
        string description = typeof(WebTools).GetMethod(nameof(WebTools.ReadRenderedPage))!
            .GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
            .Cast<System.ComponentModel.DescriptionAttribute>().Single().Description;

        Assert.That(description, Does.Not.Contain("Tavily").And.Not.Contain("credit"));
        Assert.That(description, Does.Contain("slower than FetchUrl").And.Contain("every request the page makes"));
    }

    [Test]
    public async Task ReadRenderedPage_RefusesAPrivateAddress_BeforeSendingItAnywhere()
    {
        // The guard that FetchUrl has: an internal URL must not be handed to a third party either.
        var tavily = new StubHandler(_ => Html(ExtractJson, "application/json"));
        var renderer = new TavilyPageRenderer("k", new HttpClient(tavily));

        string result = await WebToolsOn(_ => Html(""), dns: (_, _) => Task.FromResult(new[] { IPAddress.Parse("10.0.0.5") }), renderer: renderer)
            .ReadRenderedPage("https://intranet.example/secret");

        Assert.That(result, Does.StartWith("Error"));
        Assert.That(tavily.Requests, Is.Empty);
    }

    [Test]
    public void RendererFactory_PicksTavilyWhenTheKeyIsSet()
    {
        Assert.That(PageRendererFactory.Create(new AgentOptions { TavilyApiKey = "tvly-key" }), Is.TypeOf<TavilyPageRenderer>());
        Assert.That(PageRendererFactory.Create(new AgentOptions()), Is.TypeOf<UnavailablePageRenderer>());
    }
}
