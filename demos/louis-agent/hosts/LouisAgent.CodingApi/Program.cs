using LouisAgent.Api.Config;
using LouisAgent.Api.Evals;
using LouisAgent.Core.Web;
using LouisAgent.Core.Anthropic;
using LouisAgent.Core.Ollama;
using LouisAgent.Core.OpenAi;
using LouisAgent.Core.Telemetry;
using LouisAgent.Accounts;
using LouisAgent.Accounts.Config;
using LouisAgent.Api;
using LouisAgent.Api.Site;
using LouisAgent.Coding;
using LouisAgent.Core.Hosting;

// The coding agent's API. It loads what this demo's catalogue names, and serves the code-review app, with the
// framework's accounts (F61) when ACCOUNTS_SQL_URL is set.
var services = new HostServices(WebAccess.Create, OtlpTelemetry.Instance,
    Providers: [AnthropicProvider.Instance, OllamaProvider.Instance, OpenAiCompatibleProvider.Instance]);
var host = AgentHost.Build(CodingCatalog.Create(), services);
var builder = WebApplication.CreateBuilder(args);
builder.AddLouisAgentApi(host);
builder.AddLouisAgentAccounts(AccountsOptions.FromEnvironment());
// The evals of the demo's agents (F126.5.3), the triage team's among them, as the sites' hosts take theirs: read at
// start-up, run one at a time in the background, the judge on the host's provider and its ledger.
var evals = EvalsOptions.FromEnvironment();
builder.AddLouisAgentEvals(host, EvalsStartup.JudgeClient(host, services.ClientFactory(), evals), evals);
var app = builder.Build();

// The accounts first (the tenant from the host, who the cookie says), then the key check, which takes a signed-in
// person in place of the key. The agent can run shell commands and write files, so the key check comes before any
// route.
app.UseLouisAgentAccounts();
app.UseLouisAgentKey();

// The code-review app's files, and any path that isn't an API route loads it, such as /chat/{sessionId}. Its pages are
// the chat and the admin, none meant for search, so each answers the plain shell; the framework's site has its own
// host since F124.
app.MapStaticAssets();
app.MapLouisAgentSite(typeof(LouisAgent.CodeReview.App).Assembly, services =>
{
    services.AddSingleton(new LouisAgent.Web.Text.ChatText());
    services.AddSingleton(new HttpClient());
});

app.MapLouisAgentAccounts();
app.MapLouisAgentApi(host);

// The evals' routes, on the API's group, the admin's alone; the code-review admin shows their runs.
string? adminKey = AdminOptions.FromEnvironment().AdminKey;
app.MapLouisAgentHostGroup().MapLouisAgentEvals(adminKey);
app.Run();
