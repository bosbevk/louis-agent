using LouisAgent.Api;
using LouisAgent.Api.Config;
using LouisAgent.Api.Evals;
using LouisAgent.Api.Site;
using LouisAgent.Core.Anthropic;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Telemetry;
using LouisAgent.Core.Web;
using LouisAgent.SiteFrameworkApi;

// The framework's own site's host: the framework's API running the framework Orbi on its two packs, and the site's
// pages, each drawn here as HTML for a search engine.
var services = new HostServices(WebAccess.Create, OtlpTelemetry.Instance, Providers: [AnthropicProvider.Instance]);
var host = AgentHost.Build(FrameworkCatalog.Create(), services);
var builder = WebApplication.CreateBuilder(args);
builder.AddLouisAgentApi(host);
// The framework Orbi's evals (F126.5.2), as louis-agent.dev's host takes its agents': read at start-up, run one at a
// time in the background, the judge on the host's provider and its ledger.
var evals = EvalsOptions.FromEnvironment();
builder.AddLouisAgentEvals(host, EvalsStartup.JudgeClient(host, services.ClientFactory(), evals), evals);
var app = builder.Build();

// The key check comes before any route; the open door (OPEN_IDENTITIES: the framework Orbi) lets a visitor in
// without one.
app.UseLouisAgentKey();

app.MapStaticAssets();
app.MapLouisAgentSite(typeof(LouisAgent.SiteFramework.App).Assembly, FrameworkPageServices.Register,
    home: LouisAgent.SiteFramework.FrameworkAreas.Home);

app.MapLouisAgentApi(host);

// The evals' routes, on the API's group, the admin's alone: the framework site has no admin pages, so a run is
// started and read over the API.
string? adminKey = AdminOptions.FromEnvironment().AdminKey;
app.MapLouisAgentHostGroup().MapLouisAgentEvals(adminKey);
app.Run();
