namespace LouisAgent.Composition.Messages;

using System.ComponentModel;

/// <summary>The composition root's own exception texts (docs/CLAUDE.md, *Messages and logging*: each project keeps its own).</summary>
public enum CompositionErrorMessage
{
    [Description("Unknown identity '{Name}' in IDENTITIES. Supported: {Supported}.")]
    UnknownIdentity,

    [Description("Unknown pack '{Name}' in PACKS. Supported: {Supported}.")]
    UnknownPack,
}
