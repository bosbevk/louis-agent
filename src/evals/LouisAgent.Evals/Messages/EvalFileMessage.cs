using System.ComponentModel;
using LouisAgent.Composition.Messages;
using LouisAgent.Evals.Messages.Holes.Facts;
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

    [Description("'{Words}' is no agent's address. Write an identity, as 'acme', or one of its roles, as 'acme.cook'.")]
    [Holes(typeof(NotAnAddressHoles))]
    NotAnAddress,

    [Description("'{Words}': never-says names the text the answer must not hold.")]
    [Holes(typeof(NeverSaysNeedsTextHoles))]
    NeverSaysNeedsText,

    [Description("{Place} (line {Line}): {Reason}")]
    [Holes(typeof(FaultOfShapeHoles))]
    FaultOfShape,

    [Description("{Place}: {Reason}")]
    [Holes(typeof(FaultOfMeaningHoles))]
    FaultOfMeaning,

    [Description("An eval file holds at least one conversation.")]
    NoConversations,

    [Description("A conversation needs an id, a name used once in its file.")]
    NoId,

    [Description("'{Id}' is used twice. A conversation's id is used once in its file.")]
    [Holes(typeof(IdUsedTwiceHoles))]
    IdUsedTwice,

    [Description("A conversation has at least one turn.")]
    NoTurns,

    [Description("The turn asks nothing. Write what the person asks.")]
    EmptyAsk,

    [Description("A criterion is empty. Write it as a plain sentence the judge can mark.")]
    EmptyCriterion,

    [Description("'{Page}' is no page. A page is an address on the site, starting with /, as '/identities'.")]
    [Holes(typeof(PageNotAnAddressHoles))]
    PageNotAnAddress,

    [Description("The conversation checks nothing, so it would pass whatever the agent said. " +
                 "Give a turn a fact or a criterion.")]
    ChecksNothing
}
