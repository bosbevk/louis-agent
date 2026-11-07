using System.ComponentModel;

namespace LouisAgent.AcpServer.Messages;

/// <summary>Log lines for Rider's tool discovery and its MCP client (CL57, out of core), written to stderr and the run file.</summary>
public enum RiderLogMessage
{
    [Description("Starting MCP tool discovery from {Endpoint}")]
    DiscoveryStarting,

    [Description("Startup task exception: {Error}")]
    StartupFailed,

    [Description("MCP discovery disabled or not configured")]
    DiscoveryDisabled,

    [Description("No tools discovered from Rider MCP server")]
    NoToolsDiscovered,

    [Description("Failed to register tool '{Name}': {Reason}")]
    RegisterFailed,

    [Description("Discovered and registered {Registered}/{Count} tools from Rider")]
    Registered,

    [Description("Tool discovery failed: {Reason}")]
    DiscoveryFailed,

    [Description("Failed to connect to Rider at {Endpoint}: {Reason}")]
    McpConnectFailed,

    [Description("Tool discovery error: {Reason}")]
    McpDiscoveryError,

    [Description("Initialize failed: {Status}")]
    McpInitializeFailed,

    [Description("Response: {Body}")] McpResponse,

    [Description("Session established: {Session}")]
    McpSessionEstablished,

    [Description("Discovered {Count} tools from Rider")]
    McpToolsDiscovered,

    [Description("Stream discovery failed: {Reason}")]
    McpStreamDiscoveryFailed,

    [Description("SSE response (partial): {Text}")]
    McpSsePartial,

    [Description("SSE discovery failed: {Reason}")]
    McpSseDiscoveryFailed,
}
