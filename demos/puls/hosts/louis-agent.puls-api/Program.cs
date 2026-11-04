using LouisAgent.Api;
using LouisAgent.Core.Hosting;
using LouisAgent.HostPacks;

// Puls's API: the identities and packs its settings name, serving Puls's web app.
var host = AgentHost.Build(Plugins.FromEnvironment());
var builder = WebApplication.CreateBuilder(args);
builder.AddLouisAgentApi(host);
var app = builder.Build();

// The agent can run shell commands and write files, so the key check comes first.
app.UseLouisAgentKey();

// The web app's files, and any path that isn't an API route loads it, such as /gesels/{sessionId}.
app.MapStaticAssets();
app.MapFallbackToFile("index.html");

app.MapLouisAgentApi(host);
app.Run();
