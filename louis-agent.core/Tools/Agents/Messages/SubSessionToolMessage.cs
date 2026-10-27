
using System.ComponentModel;

namespace LouisAgent.Core.Tools.Agents.Messages;
// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Strings for the sub-session and minion tools, returned to the model: what went wrong and what to do instead.</summary>
public enum SubSessionToolMessage
{
    [Description("No attachment named '{Name}' in this conversation. Attached: {Attached}.")]
    AttachmentUnknown,

    [Description("Error: no peer agent named '{Agent}'. Peers (AGENT_PEERS): {Peers}.")]
    UnknownPeer,

    [Description("Error: this instance has more than one peer agent; name one of them: {Peers}.")]
    PeerNameNeeded,

    [Description(
        "Error: this turn has already made {Max} sub-session calls (starts and continues), the most one turn may. Finish with what you have, or ask the user.")]
    SubSessionLimit,

    [Description(
        "Error: this session keeps no sub-session open (it isn't a chat). Start a new one with StartSubSession.")]
    SubSessionNoneKept,

    [Description(
        "Error: no open sub-session '{Handle}'. Open: {Open}. Continue one of those, or start a new one with StartSubSession.")]
    SubSessionUnknownHandle,

    [Description("(none)")] SubSessionNoneOpen,

    [Description(
        "Error: sub-session {Handle} on {Agent} is no longer open on the peer (dropped as idle, or the peer restarted). Start a new one with StartSubSession, with the whole context the follow-up needs.")]
    SubSessionGone,

    [Description(
        "Error: {Max} sub-sessions have started in the last minute outside a chat turn, the most allowed. Wait a minute and try again.")]
    SubSessionRateLimit,

    [Description("Error: {Agent}'s reply couldn't be read ({Reason}). Try again once, or do the task another way.")]
    SubSessionUnreadable,

    [Description("Error: {Agent} refused the sub-session (HTTP {Status}): {Reason}")]
    SubSessionRefused,

    [Description("Error: the sub-session on {Agent} failed: {Reason}")]
    SubSessionFailed,

    [Description(
        "Error: the sub-session on {Agent} didn't finish within {Minutes} minutes and was stopped. Try a smaller task.")]
    SubSessionTimedOut,

    [Description("{Answer}\n\n[Sub-session on {Agent} ({Toolsets}): {Usage}]")]
    SubSessionAnswer,

    [Description(
        "{Answer}\n\n[Sub-session {Handle} on {Agent} ({Toolsets}; {Role}): {Usage}. It stays open: a follow-up on this " +
        "work goes to ContinueSubSession(\"{Handle}\", message), not to a new sub-session.]")]
    SubSessionAnswerKept,

    [Description("default role")] SubSessionDefaultRole,

    [Description(
        "Error: '{Role}' isn't a minion role (a minion role is <product>-minion-<function>; your role file lists them).")]
    MinionRoleInvalid,

    [Description(
        "Error: '{Tool}' is never handed to a minion: a minion gets one of your working tools, not delegation or skills.")]
    MinionToolNeverHandedDown,

    [Description("Error: you don't hold a '{Tool}' tool, so you can't hand it to a minion. Your tools: {Tools}.")]
    MinionToolNotMine,

    [Description(
        "Error: no model is configured for the '{Role}' minion (MINION_LLMS names neither it nor *). Do the task yourself.")]
    MinionNoLlm,

    [Description(
        "Error: no stronger model is left for the '{Role}' minion (its list: {Models}). Treat the answer as unconfirmed, or do the task yourself.")]
    MinionNoStronger,

    [Description(
        "Error: this user turn has already used {Max} minions, the most one turn may across every specialist. Finish with what you have.")]
    MinionLimit,

    [Description("{Answer}\n\n[Minion {Role} on {Llm} with {Tool}: {Usage}]")]
    MinionAnswer,

    [Description(
        "You stopped without writing an answer (usually: all your tool rounds were used). Don't call any more tools: answer the task now from what you found, and say what you couldn't finish.")]
    SubSessionAskForAnswer,

    [Description(
        "Error: the sub-session on {Agent} ended without an answer twice (usually: it used all its tool rounds). Don't send the same task again: split it into smaller tasks, or tell the user what couldn't be done. [{Usage}]")]
    SubSessionNoAnswer,

    [Description("{Requests} requests, {Tokens} tokens, {Cost}")]
    SubSessionUsage,

    [Description("usage not reported")] SubSessionNoUsage,

    [Description("cost unknown")] SubSessionCostUnknown,
}
