using System.ComponentModel;

namespace LouisAgent.Core.Delegation.Messages;

/// <summary>
/// What the ask and minion tools tell the model: what went wrong and what to do instead, and the line under an asked
/// agent's answer that says who gave it and what it did.
/// </summary>
/// <remarks>
/// A footer names the child, never what it used: that goes to the ledger and the parent's turn. Numbers that change
/// from run to run would make the parent's next request differ, and a replay could no longer match it (FX27).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public enum SubSessionToolMessage
{
    [Description("No attachment named '{Name}' in this conversation. Attached: {Attached}.")]
    AttachmentUnknown,

    [Description("Error: no peer agent named '{Agent}'. Peers (AGENT_PEERS): {Peers}.")]
    UnknownPeer,

    [Description("Error: this instance has more than one peer agent; name one of them: {Peers}.")]
    PeerNameNeeded,

    [Description(
        "Error: this turn has already asked other agents {Max} times, the most one turn may. Finish with what you " +
        "have, or ask the user.")]
    SubSessionLimit,

    [Description(
        "Error: {Max} sub-sessions have started in the last minute outside a chat turn, the most allowed. Wait a " +
        "minute and try again.")]
    SubSessionRateLimit,

    [Description("Error: {Agent}'s reply couldn't be read ({Reason}). Try again once, or do the task another way.")]
    SubSessionUnreadable,

    [Description("Error: {Agent} refused the sub-session (HTTP {Status}): {Reason}")]
    SubSessionRefused,

    [Description("Error: the sub-session on {Agent} failed: {Reason}")]
    SubSessionFailed,

    [Description(
        "Error: the sub-session on {Agent} didn't finish within {Minutes} minutes and was stopped. Try a smaller " +
        "task.")]
    SubSessionTimedOut,


    [Description(
        "Error: '{Role}' isn't a minion role (a minion role is <product>-minion-<function>; your role file lists " +
        "them).")]
    MinionRoleInvalid,

    [Description(
        "Error: '{Tool}' is never handed to a minion: a minion gets one of your working tools, not delegation or " +
        "skills.")]
    MinionToolNeverHandedDown,

    [Description("Error: you don't hold a '{Tool}' tool, so you can't hand it to a minion. Your tools: {Tools}.")]
    MinionToolNotMine,

    [Description(
        "Error: no model is configured for the '{Role}' minion (MINION_LLMS names neither it nor *). Do the task " +
        "yourself.")]
    MinionNoLlm,

    [Description(
        "Error: no stronger model is left for the '{Role}' minion (its list: {Models}). Treat the answer as " +
        "unconfirmed, or do the task yourself.")]
    MinionNoStronger,

    [Description(
        "Error: this user turn has already used {Max} minions, the most one turn may across every specialist. " +
        "Finish with what you have.")]
    MinionLimit,

    [Description("{Answer}\n\n[Minion {Role} on {Llm} with {Tool}]")]
    MinionAnswer,

    [Description(
        "You stopped without writing an answer (usually: all your tool rounds were used). Don't call any more " +
        "tools: answer the task now from what you found, and say what you couldn't finish.")]
    SubSessionAskForAnswer,

    [Description(
        "Error: the sub-session on {Agent} ended without an answer twice (usually: it used all its tool rounds). " +
        "Don't send the same task again: split it into smaller tasks, or tell the user what couldn't be done.")]
    SubSessionNoAnswer,

    // An address (F127.3.1): who it reaches, or why it reaches no one.

    [Description("Error: no agent here is called '{Identity}'. The agents here: {Identities}.")]
    AddressUnknownIdentity,

    [Description("Error: {Identity} has no voice of its own to answer in. Ask one of its roles: {Roles}.")]
    AddressNoVoice,

    [Description(
        "Error: {Peer} has no agent named {Identity}, so nothing was asked. It has: {Identities}.")]
    PeerUnknownIdentity,

    [Description(
        "Error: {Peer} refused this server's key, so nothing was asked. Tell the person their question couldn't " +
        "reach {Peer} now.")]
    PeerKeyRefused,

    [Description(
        "Error: {Peer} didn't answer ({Reason}), so nothing was asked. Tell the person it couldn't be reached now.")]
    PeerUnreachable,

    [Description("Error: {Identity} has no role '{Role}'. Its roles: {Roles}.")]
    AddressUnknownRole,

    [Description("Error: {Identity} has no minion '{Role}'. Its minions: {Minions}.")]
    AddressUnknownMinion,

    [Description(
        "Error: '{Address}' names another server, and a minion runs on this instance only. Give its address " +
        "without the server.")]
    MinionOnAnotherServer,

    [Description("Error: {Asker} may not call {Address}. The agents it may call: {Calls}.")]
    AddressNotCallable,

    [Description(
        "Error: '{Address}' is no agent's address. Write the agent's name, or its name and a role after a dot " +
        "(acme.cook), in lowercase letters, digits and dashes.")]
    AddressInvalid,

    [Description("Error: this chat speaks for no agent, so it may ask none.")]
    AskerNone,

    [Description("{Answer}\n\n[Answered by {Address}, did: {Did}]")]
    AgentAnswer,

    [Description(
        "{Answer}\n\n[Answered by {Address}, did: {Did}. A new conversation: the earlier one had closed, so it " +
        "remembers none of it]")]
    AgentAnswerAnew,

    [Description("no tools")] AgentDidNothing,

    [Description("{Tool}: {Result}")] AgentDidCall,

    [Description("{Tool} failed: {Result}")]
    AgentDidCallFailed,

    [Description(
        "Error: {Agent} is still answering an earlier message in this chat, so this one wasn't sent; it stays open. " +
        "Wait for that answer, then send this again.")]
    SubSessionBusy,

    [Description("--- Answer {Number} ---")]
    AgentWorkBegan,

    [Description("Asked: {Text}")] AgentWorkAsked,

    [Description("Said: {Text}")] AgentWorkSaid,

    [Description("Called {Tool} with {Input}")]
    AgentWorkCalled,

    [Description("{Tool} returned: {Text}")]
    AgentWorkReturned,

    [Description("{Tool} failed: {Text}")] AgentWorkFailed,

    [Description("[The earliest {Cut} characters are left out. What follows is the latest part.]")]
    AgentWorkCut,

    [Description("Error: no agent was asked in this chat, so there is no work to read.")]
    AgentWorkNoneAsked,

    [Description(
        "Error: no agent was asked at '{Address}' in this chat, so it has no work to read. Asked: {Asked}.")]
    AgentWorkNotAsked,

    [Description("# Agents you may ask")] AgentsYouMayAskHeading,

    [Description("These are the addresses you may pass to `AskAgent`. Any other is refused.")]
    AgentsYouMayAskIntro,

    [Description("- `{Address}`")] AgentYouMayAsk
}
