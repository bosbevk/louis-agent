using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Sessions;
using LouisAgent.Composition.Messages;
using LouisAgent.Choices;
using LouisAgent.Api.Messages;
using LouisAgent.Api.Sessions.Streaming;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Agent.Toolsets;
using System.Collections.Concurrent;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Roles;
using LouisAgent.Core.Delegation;
using LouisAgent.Core.Delegation.Addresses;

namespace LouisAgent.Api.Sessions;

/// <summary>
/// In-memory sessions (like the ACP server); idle ones are dropped so memory doesn't grow forever. Each session gets
/// the engine of the toolsets it asks for (F5), from <see cref="AgentEngineFactory"/>.
/// </summary>
internal sealed class AgentSessions(AgentEngineFactory engines)
{
    /// <summary>What a session created without <c>toolsets</c> loads (F5 decision: self-extension only).</summary>
    public static readonly IReadOnlyList<string> DefaultToolsets = [CoreToolset.Skills.Wire()];

    /// <summary>
    /// The toolset that asks another agent (F127.3.1); every session may load it but a minion's, which asks no one
    /// (F127.5).
    /// </summary>
    public static readonly string AgentsToolset = CoreToolset.Agents.Wire();

    /// <summary>
    /// A session nobody has used for this long is dropped at the next create, with the sub-sessions it kept.
    /// </summary>
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromHours(4);

    private readonly ConcurrentDictionary<string, AgentSession> _sessions = new();

    /// <summary>
    /// Opens a session with <paramref name="toolsets"/> (null: <see cref="DefaultToolsets"/>; empty: none) and
    /// <paramref name="role"/> (null: the default voice). At the top a session is a voice; below it, its level is the
    /// folder its role is in (F127.5): an identity's voice asked by another agent opens as a voice, with its own packs
    /// when none are sent, a specialist as a specialist, a minion as a minion. Throws <see cref="ToolsetException"/>
    /// for an unknown or unavailable toolset, a depth past AGENT_MAX_DEPTH, or a minion asking for
    /// <see cref="AgentsToolset"/>; <see cref="Core.Skills.Roles.RoleException"/> for an unknown role or an agent
    /// already in its chain of asks; and <see cref="Core.Hosting.SessionLlms.SessionLlmException"/> for an LLM
    /// profile this instance doesn't offer (<paramref name="llm"/>, F30; null: its own model), all before anything is
    /// created. A minion names its one <paramref name="tool"/>, which must be its parent session's
    /// (<paramref name="tags"/>' parent, found here: minions run on their asker's own instance) and never one of
    /// <see cref="MinionAsk.NeverHandedDown"/>; its toolsets, when none are sent, are the one that builds the tool.
    /// Every other role refuses a tool.
    /// </summary>
    public AgentSession Create(IEnumerable<string>? toolsets = null, UsageTags? tags = null, int depth = 0,
        string? role = null,
        UserPlace? place = null, string? llm = null, string? tool = null, StreamFormat format = StreamFormat.Claude,
        SessionReplay? replay = null, SessionOpener? opener = null, string? identity = null)
    {
        // Who the session is (F109.3): the identity it names, whose voice it opens in unless it names a role of its
        // own; an identity this instance doesn't run is refused by name, with the ones it does.
        var who = FindIdentity(identity);
        if (who is not null && string.IsNullOrWhiteSpace(role)) role = who.DefaultVoice;
        int maxDepth = engines.Context.Options.AgentMaxDepth;
        if (depth < 0 || depth > maxDepth)
            throw new ToolsetException(MessageText.Format(ApiErrorMessage.SessionDepthInvalid, new { Max = maxDepth }));
        if (string.IsNullOrWhiteSpace(tool)) tool = null;
        // The prompt's level: a voice at the top, below it the role's own folder.
        var level = LevelOf(role, depth, tool);
        if (level == RoleLevel.Minion) CheckMinion(tags, tool, role);
        else if (tool is not null)
            throw new ToolsetException(MessageText.Format(ApiErrorMessage.ToolOnlyForMinions,
                new { Role = role ?? "the default voice" }));
        // A minion names its tool; the toolset that builds it is implied (AskAgent sends none for a minion).
        var names = ToolsetCatalog.Normalise(level == RoleLevel.Minion && toolsets is null
            ?
            [
                engines.Catalog.ToolsetOf(tool!)
                ?? throw new ToolsetException(MessageText.Format(ApiErrorMessage.NoToolsetForTool, new { Tool = tool }))
            ]
            : toolsets ?? Unchosen(who, level, depth));
        // A role's own toolsets (F33: the cook's `shops?`, from its header's metadata, CL27) come with it, beside what
        // its parent passed; then every name, optional or not, is held to the same rules.
        names = ToolsetCatalog.Normalise(names.Concat(SkillLoader.ToolsetsOf(role, level)));
        var plain = names.Select(ToolsetCatalog.NameOf).ToList();
        if (level == RoleLevel.Minion && plain.Contains(AgentsToolset))
            throw new ToolsetException(MessageText.Format(ApiErrorMessage.MinionCannotAsk,
                new { Toolset = AgentsToolset }));
        if (string.IsNullOrEmpty(role)) role = null;
        // A sub-session's role is named by its parent (CL10); with none it gets the default mode's voice, no routing.
        if (string.IsNullOrWhiteSpace(llm)) llm = null;
        // Who it runs as, and the chain of asks it opens in: an agent already in it is refused (F127.5).
        var runsAs = Who(who ?? engines.Catalog.IdentityOf(role));
        var chain = ChainOf(tags, AgentAddresses.ChainKeyOf(role, runsAs?.Name));
        // What the session loads: an optional toolset this instance lacks is left out, and the session says so.
        names = engines.Catalog.Resolve(names, engines.Context).Select(t => t.Name).ToList();
        var engine = engines.GetOrCreate(names, role, level, llm, tool);

        DropIdle(DateTimeOffset.UtcNow);

        var session = new AgentSession($"sess_{Guid.NewGuid():N}", engine, names, depth, tags ?? UsageTags.None, role,
            place, llm, tool, format, replay, opener, runsAs)
        { Chain = chain };
        _sessions[session.Id] = session;
        return session;
    }

    /// <summary>
    /// The level a session's prompt is read at (F127.5, Louis: "depth decides" only how deep): a voice at the top;
    /// below it the folder its role is in, and a session that names a tool but no role we know is taken for a minion,
    /// so the minion's checks and the factory refuse it. Depth keeps its own guard.
    /// </summary>
    /// <remarks>Guard clauses (Fowler): the top, a role we know, then what the tool says.</remarks>
    private static RoleLevel LevelOf(string? role, int depth, string? tool)
    {
        if (depth == 0) return RoleLevel.Agent;
        if (RoleCatalog.LevelOf(role) is { } level) return level;
        return tool is null ? RoleLevel.Specialist : RoleLevel.Minion;
    }

    /// <summary>
    /// The chain of asks a session opens in (F127.5): its parent's, when this instance has the parent, then its own
    /// agent. An agent already in it is refused, so the asks can't go round in a loop.
    /// </summary>
    /// <remarks>Guard clauses (Fowler): no agent to add, one already there, then the chain one longer.</remarks>
    private IReadOnlyList<string> ChainOf(UsageTags? tags, string? agent)
    {
        string? parentId = tags?.Parent?.Split('#')[0];
        IReadOnlyList<string> above = parentId is null ? [] : Get(parentId)?.Chain ?? [];
        if (agent is null) return above;
        if (above.Contains(agent))
            throw new RoleException(MessageText.Format(ApiErrorMessage.AgentAlreadyInChain,
                new
                {
                    Agent = agent,
                    Chain = string.Join(" > ", above)
                }));
        return [.. above, agent];
    }

    /// <summary>
    /// The toolsets of a session nobody chose any for: an identity's voice asked by another agent gets its own
    /// packs, as a visitor at the door does; every other session the instance's default.
    /// </summary>
    private IEnumerable<string> Unchosen(IAgentIdentity? who, RoleLevel level, int depth) =>
        who is not null && level == RoleLevel.Agent && depth > 0 ? engines.Catalog.ToolsetsOf(who) : DefaultToolsets;

    /// <summary>Who a session in this identity runs as; none for a session of no identity.</summary>
    private static TurnIdentity? Who(IAgentIdentity? identity) =>
        identity is null ? null : TurnIdentity.Of(identity);

    /// <summary>The identity a session names, or null for none named; one this instance lacks is refused.</summary>
    /// <remarks>Guard clauses: none named, a known one, then the refusal.</remarks>
    private IAgentIdentity? FindIdentity(string? identity)
    {
        if (string.IsNullOrWhiteSpace(identity)) return null;
        if (engines.Catalog.Identity(identity) is { } who) return who;
        throw new RoleException(MessageText.Format(ApiErrorMessage.UnknownIdentity,
            new
            {
                Name = identity,
                Available = engines.Catalog.Identities.Count == 0
                    ? "none"
                    : string.Join(", ", engines.Catalog.Identities.Select(i => i.Name))
            }));
    }

    /// <summary>
    /// A minion's checks (F30), in code and before any HTTP or engine: capability delegation means it can never do
    /// more than the specialist that sent it, which hands it one tool of its own (Louis: no list per role; the
    /// specialist's toolbox is the only one). The role itself (a minion role, not a specialist's or the base) is the
    /// factory's check, as for every role.
    /// </summary>
    private void CheckMinion(UsageTags? tags, string? tool, string? role)
    {
        if (tool is null)
            throw new ToolsetException(MessageText.Format(ApiErrorMessage.MinionNeedsTool,
                new { Role = role ?? "(none)" }));
        if (string.IsNullOrWhiteSpace(tags?.Parent))
            throw new ToolsetException(MessageText.Format(ApiErrorMessage.MinionNeedsParent,
                new { Example = "sess_…#3" }));
        string parentId = tags.Parent.Split('#')[0];
        if (Get(parentId) is not { } parent)
            throw new ToolsetException(MessageText.Format(ApiErrorMessage.MinionParentUnknown,
                new { Session = parentId }));
        if (MinionAsk.NeverHandedDown.Contains(tool))
            throw new ToolsetException(MessageText.Format(ApiErrorMessage.ToolNeverHandedDown, new { Tool = tool }));
        if (!parent.Engine.Tools.Any(t => string.Equals(t.Name, tool, StringComparison.Ordinal)))
            throw new ToolsetException(MessageText.Format(ApiErrorMessage.ParentLacksTool, new { Tool = tool }));
    }

    /// <summary>The session by its id, if this instance has it.</summary>
    public AgentSession? Get(string id) => _sessions.GetValueOrDefault(id);

    /// <summary>Ends a session and the sub-sessions it kept; false when there was none by that id.</summary>
    public bool Remove(string id)
    {
        if (!_sessions.TryRemove(id, out var session)) return false;
        Forget(session);
        return true;
    }

    /// <summary>
    /// Drops every session idle at <paramref name="now"/> (F26: with the sub-sessions it kept); returns how many.
    /// </summary>
    internal int DropIdle(DateTimeOffset now)
    {
        int dropped = 0;
        foreach (var idle in _sessions.Values.Where(s => !s.IsBusy && now - s.LastActivity > IdleTimeout))
        {
            if (!_sessions.TryRemove(idle.Id, out var session)) continue;
            Forget(session);
            dropped++;
        }

        return dropped;
    }

    /// <summary>
    /// Stops the session's turn and closes its kept sub-sessions on the peer; the closing never blocks the caller.
    /// </summary>
    private static void Forget(AgentSession session)
    {
        session.Cancel();
        _ = session.SubSessions.CloseAllAsync();
    }
}
