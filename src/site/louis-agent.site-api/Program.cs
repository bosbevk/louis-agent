using LouisAgent.Api;
using LouisAgent.Api.Config;
using LouisAgent.Api.Site;
using LouisAgent.Core.Anthropic;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Telemetry;
using LouisAgent.Core.Web;
using LouisAgent.SiteApi;
using LouisAgent.SiteApi.Resumes;

// The framework's site's host (F124): the site's pages as HTML, and the API the guide in the corner runs on. It loads
// the guide and its three packs, so a visitor reaches nothing else.
var host = AgentHost.Build(SiteCatalog.Create(), new HostServices(WebAccess.Create, OtlpTelemetry.Instance,
    Providers: [AnthropicProvider.Instance]));
var builder = WebApplication.CreateBuilder(args);
builder.AddLouisAgentApi(host);
var app = builder.Build();

// The key check comes before any route; the open door (OPEN_IDENTITIES: the guide and the showroom's businesses)
// lets a visitor in without one.
app.UseLouisAgentKey();

// The framework's pages moved under /framework, so their old addresses are sent on first.
app.MapMovedPages();

// The site's files, and any other path loads the app: a page meant for search as its own HTML (F121), rendered with
// the words its pages read; the landing's sections sit under /home.
app.MapStaticAssets();
app.MapLouisAgentSite(typeof(LouisAgent.Site.App).Assembly, services =>
{
    services.AddSingleton(new LouisAgent.Web.Text.ChatText());
    services.AddSingleton(new HttpClient());
}, home: "home");

app.MapLouisAgentApi(host);

// The site's own routes, on the API's group: Louis's CV, read by anyone and saved from the admin (F142).
app.MapLouisAgentHostGroup().MapSiteResume(ResumeStore.FromEnvironment(), AdminOptions.FromEnvironment().AdminKey);
app.Run();
