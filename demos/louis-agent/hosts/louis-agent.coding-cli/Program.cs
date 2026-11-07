using LouisAgent.Core.Web;
using LouisAgent.Core.Telemetry;
using LouisAgent.Core.Hosting;
using LouisAgent.Cli;
using LouisAgent.Coding;

// Usage: louis-agent.coding-cli [prompt]. It loads what this demo's catalogue names.
await CliHost.RunAsync(args, CodingCatalog.Create(), new HostServices(WebAccess.Create, OtlpTelemetry.Instance));
