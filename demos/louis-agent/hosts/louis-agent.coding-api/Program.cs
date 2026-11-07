using LouisAgent.Core.Web;
using LouisAgent.Core.Telemetry;
using LouisAgent.Accounts;
using LouisAgent.Accounts.Config;
using LouisAgent.Api;
using LouisAgent.Coding;
using LouisAgent.Core.Hosting;

// The coding agent's API. It loads what this demo's catalogue names, and serves the code-review app, with the
// framework's accounts (F61) when ACCOUNTS_SQL_URL is set.
var host = AgentHost.Build(CodingCatalog.Create(), new HostServices(WebAccess.Create, OtlpTelemetry.Instance));
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
app.Run();
