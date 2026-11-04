using LouisAgent.AcpServer;
using LouisAgent.Coding;

// The coding agent inside Rider. It loads what this demo's catalogue names.
await AcpHost.RunAsync(CodingCatalog.Create());
