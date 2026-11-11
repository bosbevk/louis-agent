using LouisAgent.Composition.Agents;
using LouisAgent.Core.Sessions.SubSessions;

namespace LouisAgent.Core.Delegation.SubSessions;

/// <summary>
/// How a sub-session's answers are labelled and rolled up: a specialist (<c>SpecialistTools.Kind</c>) or a minion (F30,
/// <c>MinionTools.Kind</c>). The label names the child in errors and in the live events; the ledger model is the
/// roll-up row's.
/// </summary>
/// <param name="Address">
/// The address an asked agent was opened for (F127.3.1), kept with it; none for the others.
/// </param>
internal sealed record SubSessionKind(
    string Kind,
    string Label,
    string LedgerModel,
    Func<OpenSubSession, SubSessionMessenger.SubSessionReply, string> KeptAnswer,
    Func<SubSessionMessenger.SubSessionReply, string> Answer,
    string? Llm = null,
    AgentAddress? Address = null);
