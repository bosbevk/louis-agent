using System.ComponentModel;

namespace LouisAgent.HostPacks.Messages;

/// <summary>
/// This project's own exception texts (src/docs/CLAUDE.md, *Messages and logging*: each project keeps its own).
/// </summary>
public enum HostPacksErrorMessage
{
    [Description("Unknown identity '{Name}' in IDENTITIES. Supported: {Supported}.")]
    UnknownIdentity,

    [Description("Unknown pack '{Name}' in PACKS. Supported: {Supported}.")]
    UnknownPack,
}
