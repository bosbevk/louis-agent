
using System.ComponentModel;

namespace LouisAgent.Acp.Messages;
/// <summary>Texts the ACP server sends to Rider: protocol errors.</summary>
public enum AcpUserText
{
    [Description("Parse error: {Reason}")]
    ParseError,

    [Description("Method not found: {Method}")]
    MethodNotFound,

    [Description("Unsupported ACP protocol version: {Version}")]
    UnsupportedProtocol,

    [Description("Unknown session: {Session}")]
    UnknownSession,

    [Description("Missing required property '{Property}'.")]
    MissingProperty,
}
