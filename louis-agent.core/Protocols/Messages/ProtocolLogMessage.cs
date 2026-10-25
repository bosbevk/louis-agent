namespace LouisAgent.Core.Protocols.Messages;

using System.ComponentModel;

// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Log lines for the MCP protocol, written to the console and the run file.</summary>
public enum ProtocolLogMessage
{
    [Description("Failed to connect to Rider at {Endpoint}: {Reason}")]
    McpConnectFailed,

    [Description("Tool discovery error: {Reason}")]
    McpDiscoveryError,

    [Description("Initialize failed: {Status}")]
    McpInitializeFailed,

    [Description("Response: {Body}")]
    McpResponse,

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
