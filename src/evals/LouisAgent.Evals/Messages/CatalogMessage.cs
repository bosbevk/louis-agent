using System.ComponentModel;
using LouisAgent.Composition.Messages;
using LouisAgent.Evals.Messages.Holes.Catalog;

namespace LouisAgent.Evals.Messages;

/// <summary>
/// Why the host's eval files can't be taken as they are, said at start-up so the host stops with the reason (ADR
/// 0009: a text is a member of a message enum, its <c>[Description]</c> a template with named holes).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum CatalogMessage
{
    [Description("Two eval files are named '{Name}': '{First}' and '{Second}'. An eval's name is its file's name, " +
                 "so it is used once in a host.")]
    [Holes(typeof(NameUsedTwiceHoles))]
    NameUsedTwice,

    [Description("'{Path}' is no eval file: {Reason}")]
    [Holes(typeof(FileRefusedHoles))]
    FileRefused,

    [Description("'{Path}' proves '{Agent}', which is no identity this host has.")]
    [Holes(typeof(AgentUnknownHoles))]
    AgentUnknown
}
