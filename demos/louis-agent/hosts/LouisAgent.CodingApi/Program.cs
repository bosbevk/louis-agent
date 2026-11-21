using LouisAgent.Core.Web;
using LouisAgent.Core.Anthropic;
using LouisAgent.Core.Ollama;
using LouisAgent.Core.OpenAi;
using LouisAgent.Core.Telemetry;
using LouisAgent.Accounts;
using LouisAgent.Accounts.Config;
using LouisAgent.Api;
using LouisAgent.Api.Site;
using LouisAgent.Api.Evals;
using LouisAgent.Coding;
using LouisAgent.Core.Hosting;

// The coding agent's API. It loads what this demo's catalogue names, and serves the code-review app, with the
// framework's accounts (F61) when ACCOUNTS_SQL_URL is set.
var host = AgentHost.Build(CodingCatalog.Create(), new HostServices(WebAccess.Create, OtlpTelemetry.Instance,
    Providers: [AnthropicProvider.Instance, OllamaProvider.Instance, OpenAiCompatibleProvider.Instance]));
var builder = WebApplication.CreateBuilder(args);
builder.AddLouisAgentApi(host);
builder.AddLouisAgentAccounts(AccountsOptions.FromEnvironment());
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
app.MapLouisAgentEvals(host);
app.Run();
