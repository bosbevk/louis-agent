using LouisAgent.Core.Web;
using LouisAgent.Core.Telemetry;
using LouisAgent.Core.Hosting;
using LouisAgent.Coding;
using LouisAgent.McpServer;

// The coding agent's tools for an MCP client. It publishes what this demo's catalogue names.
await McpHost.RunAsync(args, CodingCatalog.Create(), new HostServices(WebAccess.Create, OtlpTelemetry.Instance));
