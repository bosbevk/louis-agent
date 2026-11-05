using LouisAgent.Api;
using LouisAgent.Coding;
using LouisAgent.Core.Hosting;

// The coding agent's API. It loads what this demo's catalogue names, and serves the code-review app.
var host = AgentHost.Build(CodingCatalog.Create());
var builder = WebApplication.CreateBuilder(args);
builder.AddLouisAgentApi(host);
var app = builder.Build();

// The agent can run shell commands and write files, so the key check comes first.
app.UseLouisAgentKey();

// The web app's files, and any path that isn't an API route loads it, such as /chat/{sessionId}.
app.MapStaticAssets();
app.MapFallbackToFile("index.html");

app.MapLouisAgentApi(host);
app.Run();
