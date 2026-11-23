using System.ComponentModel;
using LouisAgent.Composition.Messages;
using LouisAgent.Evals.Messages.Holes.Sessions;

namespace LouisAgent.Evals.Messages;

/// <summary>
/// What an eval says when the sessions API won't do what it asked (ADR 0009: a text is a member of a message enum,
/// its <c>[Description]</c> a template with named holes).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum SessionMessage
{
    [Description("The sessions API refused, with {Status}: {Reason}")]
    [Holes(typeof(RefusedHoles))]
    Refused
}
