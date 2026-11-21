using LouisAgent.Api;
using LouisAgent.Api.Site;
using LouisAgent.Core.Anthropic;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Telemetry;
using LouisAgent.Core.Web;
using LouisAgent.SiteFrameworkApi;

// The framework's own site's host: the framework's API running the framework Orbi on its two packs, and the site's
// pages, each drawn here as HTML for a search engine.
var host = AgentHost.Build(FrameworkCatalog.Create(), new HostServices(WebAccess.Create, OtlpTelemetry.Instance,
    Providers: [AnthropicProvider.Instance]));
var builder = WebApplication.CreateBuilder(args);
builder.AddLouisAgentApi(host);
var app = builder.Build();

// The key check comes before any route; the open door (OPEN_IDENTITIES: the framework Orbi) lets a visitor in
// without one.
app.UseLouisAgentKey();

app.MapStaticAssets();
app.MapLouisAgentSite(typeof(LouisAgent.SiteFramework.App).Assembly, FrameworkPageServices.Register,
    home: LouisAgent.SiteFramework.FrameworkAreas.Home);

app.MapLouisAgentApi(host);
app.Run();
