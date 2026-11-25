using System.ComponentModel;
using LouisAgent.Acp.Messages.Holes.UserText;
using LouisAgent.Composition.Messages;

namespace LouisAgent.Acp.Messages;

/// <summary>Texts the ACP server sends to Rider: protocol errors.</summary>
public enum AcpUserText
{
    [Description("Parse error: {Reason}")]
    [Holes(typeof(ParseErrorHoles))]
    ParseError,

    [Description("Method not found: {Method}")]
    [Holes(typeof(MethodNotFoundHoles))]
    MethodNotFound,

    [Description("Unsupported ACP protocol version: {Version}")]
    [Holes(typeof(UnsupportedProtocolHoles))]
    UnsupportedProtocol,

    [Description("Unknown session: {Session}")]
    [Holes(typeof(UnknownSessionHoles))]
    UnknownSession,

    [Description("Missing required property '{Property}'.")]
    [Holes(typeof(MissingPropertyHoles))]
    MissingProperty,
}
