namespace LouisAgent.AcpServer.Rider;

/// <summary>
/// Where Rider's MCP server is and whether to ask it for tools (CL57, out of core's settings): <c>RIDER_MCP_ENDPOINT</c>,
/// <c>RIDER_MCP_AUTO_DISCOVER</c> and <c>RIDER_MCP_PROJECT_PATH</c>, the path Rider knows the project by (in Docker the
/// host's, not the container's).
/// </summary>
/// <param name="Endpoint">Rider's MCP endpoint; its default port when the setting is unset.</param>
/// <param name="AutoDiscover">Whether to ask Rider for its tools at start-up; on unless the setting says false.</param>
/// <param name="ProjectPath">The path to tell Rider; unset, the workspace root.</param>
public sealed record RiderOptions(string Endpoint, bool AutoDiscover, string? ProjectPath)
{
    /// <summary>Rider's MCP server, on its default port, on this machine.</summary>
    public const string DefaultEndpoint = "http://127.0.0.1:64482";

    /// <summary>Reads the three settings, each with its default when unset.</summary>
    public static RiderOptions FromEnvironment(Func<string, string?>? getVariable = null)
    {
        getVariable ??= Environment.GetEnvironmentVariable;
        string? Get(string name) => string.IsNullOrWhiteSpace(getVariable(name)) ? null : getVariable(name)!.Trim();

        return new RiderOptions(
            Get("RIDER_MCP_ENDPOINT") ?? DefaultEndpoint,
            !bool.TryParse(Get("RIDER_MCP_AUTO_DISCOVER"), out bool auto) || auto,
            Get("RIDER_MCP_PROJECT_PATH"));
    }
}
