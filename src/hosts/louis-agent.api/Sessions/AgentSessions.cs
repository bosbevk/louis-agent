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
    /// The deepest session: a specialist (1) a level-0 session delegates to, a minion (2) a specialist hands one task
    /// to (F30).
    /// </summary>
    public const int MaxDepth = 2;

    /// <summary>A minion's depth (F30): one role on the minion base, one tool, never delegates.</summary>
    public const int MinionDepth = 2;

    /// <summary>
    /// The toolset a specialist hands minions with (F30): a specialist's only, never the chat's nor a minion's.
    /// </summary>
    public static readonly string MinionsToolset = CoreToolset.Minions.Wire();

    /// <summary>
    /// The toolset that opens sub-sessions (F5; named specialists in CL9); refused at <see cref="MaxDepth"/>.
    /// </summary>
    public static readonly string SpecialistsToolset = CoreToolset.Specialists.Wire();

    /// <summary>
    /// The toolset that asks another agent (F127.3.1); refused below depth 0, so an asked agent asks none.
    /// </summary>
    public static readonly string AgentsToolset = CoreToolset.Agents.Wire();

    /// <summary>
    /// A session nobody has used for this long is dropped at the next create, with the sub-sessions it kept.
    /// </summary>
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromHours(4);

    private readonly ConcurrentDictionary<string, AgentSession> _sessions = new();

    /// <summary>
    /// Opens a session with <paramref name="toolsets"/> (null: <see cref="DefaultToolsets"/>; empty: none) and
    /// <paramref name="role"/> (null: the default voice). The session's level is its depth (CL8): its role comes
    /// from that level's folder, and a sub-session gets no router (default.md): its toolsets were chosen for it and it
    /// can't delegate (F21). The one exception is an identity's voice asked at depth 1 (F127.3.1): it opens as a
    /// voice, from <c>agent/</c>, with its own packs when none are sent. Throws <see cref="ToolsetException"/> for an
    /// unknown or unavailable toolset, a depth outside 0..<see cref="MaxDepth"/>, or a sub-session asking for
    /// <see cref="SpecialistsToolset"/>, and <see cref="Core.Skills.Roles.RoleException"/> for an unknown role, and
    /// <see cref="Core.Hosting.SessionLlms.SessionLlmException"/> for an LLM profile this instance doesn't offer
    /// (<paramref name="llm"/>, F30; null: its own model), before anything is created. A minion (depth
    /// <see cref="MinionDepth"/>, F30) names its one <paramref name="tool"/>, which must be its parent session's
    /// (<paramref name="tags"/>' parent, found here: minions run on their specialist's own instance) and never one of
    /// <see cref="MinionTools.NeverHandedDown"/>; its toolsets, when none are sent, are the one that builds the tool.
    /// Every other depth refuses a tool, and only depth 1 loads <see cref="MinionsToolset"/>.
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
        if (depth is < 0 or > MaxDepth)
            throw new ToolsetException(MessageText.Format(ApiErrorMessage.SessionDepthInvalid, new { Max = MaxDepth }));
        if (string.IsNullOrWhiteSpace(tool)) tool = null;
        if (depth == MinionDepth) CheckMinion(tags, tool);
        else if (tool is not null)
            throw new ToolsetException(MessageText.Format(ApiErrorMessage.ToolOnlyForMinions,
                new
                {
                    MinionDepth,
                    Depth = depth
                }));
        // The prompt's level: the depth's, except an identity's voice asked by another agent, which stays a voice.
        var level = LevelOf(role, depth);
        // A minion names its tool; the toolset that builds it is implied (StartMinion sends none).
        var names = ToolsetCatalog.Normalise(depth == MinionDepth && toolsets is null
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
        if (depth > 0 && plain.Contains(SpecialistsToolset))
            throw new ToolsetException(MessageText.Format(ApiErrorMessage.SubSessionCannotDelegate,
                new { Toolset = SpecialistsToolset }));
        if (depth > 0 && plain.Contains(AgentsToolset))
            throw new ToolsetException(MessageText.Format(ApiErrorMessage.SubSessionCannotDelegate,
                new { Toolset = AgentsToolset }));
        if (depth == MinionDepth && plain.Contains(MinionsToolset))
            throw new ToolsetException(MessageText.Format(ApiErrorMessage.SubSessionCannotDelegate,
                new { Toolset = MinionsToolset }));
        if (depth == 0 && plain.Contains(MinionsToolset))
            throw new ToolsetException(MessageText.Format(ApiErrorMessage.MinionsOnlyForSpecialists));
        if (string.IsNullOrEmpty(role)) role = null;
        // A sub-session's role is named by its parent (CL10); with none it gets the default mode's voice, no routing.
        if (string.IsNullOrWhiteSpace(llm)) llm = null;
        // What the session loads: an optional toolset this instance lacks is left out, and the session says so.
        names = engines.Catalog.Resolve(names, engines.Context).Select(t => t.Name).ToList();
        var engine = engines.GetOrCreate(names, role, level, llm, tool);

        DropIdle(DateTimeOffset.UtcNow);

        var session = new AgentSession($"sess_{Guid.NewGuid():N}", engine, names, depth, tags ?? UsageTags.None, role,
            place, llm, tool, format, replay, opener, Who(who ?? engines.Catalog.IdentityOf(role)));
        _sessions[session.Id] = session;
        return session;
    }

    /// <summary>
    /// The level a session's prompt is read at: its depth's, as before (CL8), except an identity's voice asked by
    /// another agent (F127.3.1, Louis's pick 1 A), which opens one level down still as a voice, from <c>agent/</c>.
    /// Depth keeps its own guards: it counts how deep the call is, the level says what kind of prompt it is.
    /// </summary>
    private static RoleLevel LevelOf(string? role, int depth) =>
        depth == 1 && RoleCatalog.LevelOf(role) == RoleLevel.Agent ? RoleLevel.Agent : (RoleLevel)depth;

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
    private void CheckMinion(UsageTags? tags, string? tool)
    {
        if (tool is null)
            throw new ToolsetException(MessageText.Format(ApiErrorMessage.MinionNeedsTool,
                new { Depth = MinionDepth }));
        if (string.IsNullOrWhiteSpace(tags?.Parent))
            throw new ToolsetException(MessageText.Format(ApiErrorMessage.MinionNeedsParent,
                new { Example = "sess_…#3" }));
        string parentId = tags.Parent.Split('#')[0];
        if (Get(parentId) is not { } parent)
            throw new ToolsetException(MessageText.Format(ApiErrorMessage.MinionParentUnknown,
                new { Session = parentId }));
        if (MinionTools.NeverHandedDown.Contains(tool))
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
