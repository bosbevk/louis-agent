using LouisAgent.Composition.Messages;
using LouisAgent.Choices;
using LouisAgent.McpServer.Messages;

namespace LouisAgent.McpServer.Transport;

/// <summary>
/// Reads MCP_TRANSPORT once, where it enters, the way <c>HostAuth.Mode</c> reads AGENT_AUTH. The pattern is "parse,
/// don't validate" (Alexis King): the text becomes an enum at the edge, so the server switches on it and never compares
/// strings again. ADR 0024 (choices are enums), ADR 0009 (the error's text is a message enum).
/// </summary>
public static class McpTransportSetting
{
    /// <summary>
    /// Unset is <see cref="McpTransport.Stdio"/>; a value that names nothing stops start-up, never a quiet stdio.
    /// </summary>
    public static McpTransport Parse(string? setting)
    {
        if (!SettingChoice.TryParse(setting, out McpTransport? transport))
            throw new ArgumentException(MessageText.Format(McpServerErrorMessage.UnknownMcpTransport,
                new
                {
                    Value = setting?.Trim(),
                    Supported = string.Join(" or ", SettingChoice.Names<McpTransport>())
                }));
        return transport ?? McpTransport.Stdio;
    }
}
