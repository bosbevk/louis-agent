using System.ComponentModel;
using LouisAgent.Composition.Messages;
using LouisAgent.Evals.Messages.Holes.Files;

namespace LouisAgent.Evals.Messages;

/// <summary>
/// Why an eval file is refused: what is wrong and how to write it right (ADR 0009: a text is a member of a message
/// enum, its <c>[Description]</c> a template with named holes). A refused value is in single quotes, as the diagrams'
/// texts quote one, so no template needs an escape (Louis's pick, F126.2).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum EvalFileMessage
{
    [Description("'{Words}' is no fact. A fact starts with one of: {Kinds}.")]
    [Holes(typeof(NotAFactHoles))]
    NotAFact,

    [Description("'{Words}': {Kind} takes nothing after it.")]
    [Holes(typeof(FactTakesNothingHoles))]
    FactTakesNothing,

    [Description("'{Words}': {Kind} names one tool, as '{Kind} ReadDoc'.")]
    [Holes(typeof(FactNeedsOneToolHoles))]
    FactNeedsOneTool,

    [Description("'{Words}': under names a whole number of seconds above zero, as 'under 30s'.")]
    [Holes(typeof(UnderNeedsSecondsHoles))]
    UnderNeedsSeconds,

    [Description("'{Words}': {Kind} names an agent's address, as '{Kind} acme' or '{Kind} acme.cook'.")]
    [Holes(typeof(FactNeedsAnAddressHoles))]
    FactNeedsAnAddress,

    [Description("'{Words}': never-says names the text the answer must not hold.")]
    [Holes(typeof(NeverSaysNeedsTextHoles))]
    NeverSaysNeedsText
}
