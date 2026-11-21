namespace LouisAgent.Mcp.Transport;

/// <summary>
/// MCP_TRANSPORT: how the MCP server talks to its client (ADR 0024: a choice is an enum, not a string).
/// </summary>
public enum McpTransport
{
    /// <summary>
    /// The default: stdin and stdout, for a client on this machine that starts the server as its child process.
    /// </summary>
    Stdio,

    /// <summary>Streamable HTTP, behind an API key, for a client somewhere else on the network.</summary>
    Http,
}
