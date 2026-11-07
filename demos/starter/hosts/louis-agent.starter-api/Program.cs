using LouisAgent.Accounts;
using LouisAgent.Accounts.Config;
using LouisAgent.Api;
using LouisAgent.Api.Evals;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Anthropic;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Ollama;
using LouisAgent.Core.OpenAi;
using LouisAgent.Core.Telemetry;
using LouisAgent.Core.Web;

// The starter's API: the framework's API serving the starter site, with the framework's accounts (when
// ACCOUNTS_SQL_URL is set) and the eval studio opted in. A site names its identity and packs in the catalogue; the
// starter has none yet, so its agent is core alone.
var host = AgentHost.Build(AgentCatalog.CoreOnly, new HostServices(WebAccess.Create, OtlpTelemetry.Instance,
    Providers: [AnthropicProvider.Instance, OllamaProvider.Instance, OpenAiCompatibleProvider.Instance]));
var builder = WebApplication.CreateBuilder(args);
builder.AddLouisAgentApi(host);
builder.AddLouisAgentAccounts(AccountsOptions.FromEnvironment());
var app = builder.Build();

// The accounts first (the tenant from the host, who the cookie says), then the key check, which takes a signed-in
// person in place of the key. The agent can run shell commands and write files, so the key check comes before any route.
app.UseLouisAgentAccounts();
app.UseLouisAgentKey();

// The web app's files, and any path that isn't an API route loads it, such as /chat/{sessionId}.
app.MapStaticAssets();
app.MapFallbackToFile("index.html");

app.MapLouisAgentAccounts();
app.MapLouisAgentApi(host);
app.MapLouisAgentEvals(host);
app.Run();
