using LouisAgent.Core.Web;
using LouisAgent.Core.Telemetry;
using LouisAgent.Core.Hosting;
using LouisAgent.AcpServer;
using LouisAgent.Coding;

// The coding agent inside Rider. It loads what this demo's catalogue names.
await AcpHost.RunAsync(CodingCatalog.Create(), new HostServices(WebAccess.Create, OtlpTelemetry.Instance));
