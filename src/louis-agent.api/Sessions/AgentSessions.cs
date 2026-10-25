using LouisAgent.Api.Sessions.Streaming;
using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Agent.Toolsets;
using System.Collections.Concurrent;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Roles;
using LouisAgent.Core.Tools.Agents;
using LouisAgent.Core.Tools.Clock;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Sessions;

/// <summary>
/// In-memory sessions (like the ACP server); idle ones are dropped so memory doesn't grow forever. Each session gets the
/// engine of the toolsets it asks for (F5), from <see cref="AgentEngineFactory"/>.
/// </summary>
internal sealed class AgentSessions(AgentEngineFactory engines)
{
    /// <summary>What a session created without <c>toolsets</c> loads (F5 decision: self-extension only).</summary>
    public static readonly IReadOnlyList<string> DefaultToolsets = ["skills"];

    /// <summary>The deepest session: a specialist (1) a level-0 session delegates to, a minion (2) a specialist hands one task to (F30).</summary>
    public const int MaxDepth = 2;

    /// <summary>A minion's depth (F30): one role on the minion base, one tool, never delegates.</summary>
    public const int MinionDepth = 2;

    /// <summary>The toolset a specialist hands minions with (F30): a specialist's only, never the chat's nor a minion's.</summary>
    public const string MinionsToolset = "minions";

    /// <summary>The toolset that opens sub-sessions (F5; named specialists in CL9); refused at <see cref="MaxDepth"/>.</summary>
    public const string SpecialistsToolset = "specialists";

    /// <summary>A session nobody has used for this long is dropped at the next create, with the sub-sessions it kept.</summary>
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromHours(4);

    private readonly ConcurrentDictionary<string, AgentSession> _sessions = new();

    /// <summary>
    /// Opens a session with <paramref name="toolsets"/> (null: <see cref="DefaultToolsets"/>; empty: none) and
    /// <paramref name="role"/> (null: the default voice). The session's level is its depth (CL8): its role comes
    /// from that level's folder, and a sub-session gets no router (default.md): its toolsets were chosen for it and it
    /// can't delegate (F21). Throws <see cref="ToolsetException"/> for an unknown or unavailable
    /// toolset, a depth outside 0..<see cref="MaxDepth"/>, or a sub-session asking for <see cref="SpecialistsToolset"/>, and
    /// <see cref="Core.Skills.Roles.RoleException"/> for an unknown role, and <see cref="SessionLlmException"/> for an
    /// LLM profile this instance doesn't offer (<paramref name="llm"/>, F30; null: its own model), before anything is created.
    /// A minion (depth <see cref="MinionDepth"/>, F30) names its one <paramref name="tool"/>, which must be its parent
    /// session's (<paramref name="tags"/>' parent, found here: minions run on their specialist's own instance) and never
    /// one of <see cref="MinionTools.NeverHandedDown"/>; its toolsets, when none are sent, are the one that builds the
    /// tool. Every other depth refuses a tool, and only depth 1 loads <see cref="MinionsToolset"/>.
    /// </summary>
    public AgentSession Create(IEnumerable<string>? toolsets = null, UsageTags? tags = null, int depth = 0, string? role = null,
        UserPlace? place = null, string? llm = null, string? tool = null, StreamFormat format = StreamFormat.Claude,
        SessionReplay? replay = null)
    {
        if (depth is < 0 or > MaxDepth)
            throw new ToolsetException(MessageText.Format(ErrorMessage.SessionDepthInvalid, new { Max = MaxDepth }));
        if (string.IsNullOrWhiteSpace(tool)) tool = null;
        if (depth == MinionDepth) CheckMinion(tags, tool);
        else if (tool is not null)
            throw new ToolsetException(MessageText.Format(ErrorMessage.ToolOnlyForMinions, new { MinionDepth, Depth = depth }));
        // A minion names its tool; the toolset that builds it is implied (StartMinion sends none).
        var names = ToolsetCatalog.Normalise(depth == MinionDepth && toolsets is null ? [engines.Catalog.ToolsetOf(tool!)
                ?? throw new ToolsetException(MessageText.Format(ErrorMessage.NoToolsetForTool, new { Tool = tool }))]
            : toolsets ?? DefaultToolsets);
        // A role's own toolsets (F33: the cook's `shops?`, from its file's header) come with it, beside what its parent
        // passed; then every name, optional or not, is held to the same rules.
        names = ToolsetCatalog.Normalise(names.Concat(RoleCatalog.ToolsetsOf(role, (RoleLevel)depth)));
        var plain = names.Select(ToolsetCatalog.NameOf).ToList();
        if (depth > 0 && plain.Contains(SpecialistsToolset))
            throw new ToolsetException(MessageText.Format(ErrorMessage.SubSessionCannotDelegate, new { Toolset = SpecialistsToolset }));
        if (depth == MinionDepth && plain.Contains(MinionsToolset))
            throw new ToolsetException(MessageText.Format(ErrorMessage.SubSessionCannotDelegate, new { Toolset = MinionsToolset }));
        if (depth == 0 && plain.Contains(MinionsToolset))
            throw new ToolsetException(MessageText.Format(ErrorMessage.MinionsOnlyForSpecialists));
        if (string.IsNullOrEmpty(role)) role = null;
        // A sub-session's role is named by its parent (CL10); with none it gets the default mode's voice, no routing.
        if (string.IsNullOrWhiteSpace(llm)) llm = null;
        // What the session loads: an optional toolset this instance lacks is left out, and the session says so.
        names = engines.Catalog.Resolve(names, engines.Context).Select(t => t.Name).ToList();
        var engine = engines.GetOrCreate(names, role, (RoleLevel)depth, llm, tool);

        DropIdle(DateTimeOffset.UtcNow);

        var session = new AgentSession($"sess_{Guid.NewGuid():N}", engine, names, depth, tags ?? UsageTags.None, role, place, llm, tool, format, replay);
        _sessions[session.Id] = session;
        return session;
    }

    /// <summary>
    /// A minion's checks (F30), in code and before any HTTP or engine: capability delegation means it can never do more
    /// than the specialist that sent it, which hands it one tool of its own (Louis: no list per role; the specialist's
    /// toolbox is the only one). The role itself (a minion role, not a specialist's or the base) is the factory's check,
    /// as for every role.
    /// </summary>
    private void CheckMinion(UsageTags? tags, string? tool)
    {
        if (tool is null)
            throw new ToolsetException(MessageText.Format(ErrorMessage.MinionNeedsTool, new { Depth = MinionDepth }));
        if (string.IsNullOrWhiteSpace(tags?.Parent))
            throw new ToolsetException(MessageText.Format(ErrorMessage.MinionNeedsParent, new { Example = "sess_…#3" }));
        string parentId = tags.Parent.Split('#')[0];
        if (Get(parentId) is not { } parent)
            throw new ToolsetException(MessageText.Format(ErrorMessage.MinionParentUnknown, new { Session = parentId }));
        if (MinionTools.NeverHandedDown.Contains(tool))
            throw new ToolsetException(MessageText.Format(ErrorMessage.ToolNeverHandedDown, new { Tool = tool }));
        if (!parent.Engine.Tools.Any(t => string.Equals(t.Name, tool, StringComparison.Ordinal)))
            throw new ToolsetException(MessageText.Format(ErrorMessage.ParentLacksTool, new { Tool = tool }));
    }

    public AgentSession? Get(string id) => _sessions.GetValueOrDefault(id);

    public bool Remove(string id)
    {
        if (!_sessions.TryRemove(id, out var session)) return false;
        Forget(session);
        return true;
    }

    /// <summary>Drops every session idle at <paramref name="now"/> (F26: with the sub-sessions it kept); returns how many.</summary>
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

    /// <summary>Stops the session's turn and closes its kept sub-sessions on the peer; the closing never blocks the caller.</summary>
    private static void Forget(AgentSession session)
    {
        session.Cancel();
        _ = session.SubSessions.CloseAllAsync();
    }
}
