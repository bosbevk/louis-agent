namespace LouisAgent.AcpServer.Protocol;

/// <summary>The <c>initialize</c> reply (S, CL15, from AcpServer): the one place the protocol version and what this agent can do are stated.</summary>
internal static class AcpCapabilities
{
    /// <summary>The ACP protocol version this server speaks; a client asking for another is refused.</summary>
    public const int ProtocolVersion = 1;

    public static readonly object Result = new
    {
        protocolVersion = ProtocolVersion,
        agentCapabilities = new
        {
            loadSession = false,
            promptCapabilities = new
            {
                image = false,
                audio = false,
                embeddedContext = true
            },
            mcpCapabilities = new
            {
                http = false,
                sse = false
            },
            sessionCapabilities = new { }
        },
        agentInfo = new
        {
            name = "louis-agent",
            title = "Louis Agent",
            version = "1.0.0"
        },
        authMethods = Array.Empty<object>()
    };
}
