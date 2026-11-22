using LouisAgent.Api.Messages;
using LouisAgent.Api.Messages.Holes.Lookups;
using LouisAgent.Api.Messages.Holes.Opening;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Sessions;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Delegation;
using LouisAgent.Core.Delegation.Addresses;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Roles;
using LouisAgent.Core.Usage.Records;

namespace LouisAgent.Api.Sessions.Opening;

/// <summary>
/// Opens one session from its request, one step at a time, in the order the checks run: who it is, how deep, at what
/// level, a lookup agent's tool, its toolsets, the chain of asks, then its engine. At the top a session is a voice;
/// below it, its level is the one its role is found at (F127.5): an identity's voice asked by another agent opens as a
/// voice, with its own packs when none are sent, a subagent as a subagent, a lookup agent as a lookup agent. Throws
/// <see cref="ToolsetException"/> for an unknown or unavailable toolset, a depth past AGENT_MAX_DEPTH, or a lookup
/// agent asking for <see cref="AgentSessions.AgentsToolset"/>; <see cref="RoleException"/> for an unknown role or an
/// agent already in its chain of asks; and <c>SessionLlmException</c> for an LLM profile this instance doesn't offer,
/// all before anything is created. A lookup agent names its one tool, which must be its parent session's and never one
/// of <see cref="LookupAsk.NeverHandedDown"/>; every other role refuses a tool.
/// </summary>
/// <remarks>
/// Refactoring.Guru's <i>Replace Method with Method Object</i> (CL70, Louis's pick A of three: "why is this so ugly"):
/// the opening was one long method whose locals every check read and changed, so it couldn't be split as it stood. Its
/// locals are this class's fields, and each job is a small method, so the steps read in order and each can be read
/// alone. Chosen over a builder (it would tidy only the calls) and a chain of checks (types for rules that never change
/// at run time).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0051-flat-conditionals-and-named-endpoints.md">ADR 0051: flat conditionals</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0119-declare-name-then-pass.md">ADR 0119: declare, name, then pass</seealso>
internal sealed class SessionOpening(
    AgentEngineFactory engines,
    SessionRequest request,
    Func<string, AgentSession?> sessionOf)
{
    private IAgentIdentity? _who;
    private string? _role = request.Role;
    private string? _tool;
    private RoleLevel _level;
    private IReadOnlyList<string> _toolsets = [];

    /// <summary>The session the request asks for, every check passed; nothing is made before the last one.</summary>
    public AgentSession Open()
    {
        Identity();
        CheckDepth();
        Level();
        CheckTool();
        Toolsets();
        CheckLookupAsksNoOne();

        // A sub-session's role is named by its parent (CL10); with none it gets the default mode's voice, no routing.
        if (string.IsNullOrEmpty(_role)) _role = null;
        string? llm = string.IsNullOrWhiteSpace(request.Llm) ? null : request.Llm;
        // Who it runs as, and the chain of asks it opens in: an agent already in it is refused (F127.5).
        var runsAs = RunsAs();
        var chain = Chain(AgentAddresses.ChainKeyOf(_role, runsAs?.Name));
        var engine = Engine(llm);

        return new AgentSession($"sess_{Guid.NewGuid():N}", engine, _toolsets, request.Depth,
            request.Tags ?? UsageTags.None, _role, request.Place, llm, _tool, request.Format, request.Replay,
            request.Opener, runsAs)
        { Chain = chain };
    }

    /// <summary>
    /// Who the session is (F109.3): the identity it names, whose voice it opens in unless it names a role of its own;
    /// an identity this instance doesn't run is refused by name, with the ones it does.
    /// </summary>
    /// <remarks>Guard clauses: none named, a known one, then the refusal.</remarks>
    private void Identity()
    {
        if (string.IsNullOrWhiteSpace(request.Identity)) return;
        _who = engines.Catalog.Identity(request.Identity);
        if (_who is not null)
        {
            if (string.IsNullOrWhiteSpace(_role)) _role = _who.DefaultVoice;
            return;
        }

        string available = engines.Catalog.Identities.Count == 0
            ? "none"
            : string.Join(", ", engines.Catalog.Identities.Select(i => i.Name));
        var unknown = new UnknownIdentityHoles(request.Identity, available);
        throw new RoleException(MessageText.Format(ApiErrorMessage.UnknownIdentity, unknown));
    }

    /// <summary>No deeper than this instance allows (AGENT_MAX_DEPTH), and never above the top.</summary>
    private void CheckDepth()
    {
        int maxDepth = engines.Context.Options.AgentMaxDepth;
        if (request.Depth >= 0 && request.Depth <= maxDepth) return;
        var tooDeep = new SessionDepthInvalidHoles(maxDepth);
        throw new ToolsetException(MessageText.Format(ApiErrorMessage.SessionDepthInvalid, tooDeep));
    }

    /// <summary>
    /// The level the prompt is read at (F127.5, Louis: "depth decides" only how deep): a voice at the top; below it the
    /// level its role is found at, and a session that names a tool but no role we know is taken for a lookup agent, so
    /// its checks and the factory refuse it.
    /// </summary>
    /// <remarks>Guard clauses (Fowler): the top, a role we know, then what the tool says.</remarks>
    private void Level()
    {
        _tool = string.IsNullOrWhiteSpace(request.Tool) ? null : request.Tool;
        _level = LevelOf();
    }

    /// <summary>The level itself, worked out for <see cref="Level"/>.</summary>
    private RoleLevel LevelOf()
    {
        if (request.Depth == 0) return RoleLevel.Agent;
        if (RoleCatalog.LevelOf(_role) is { } level) return level;
        return _tool is null ? RoleLevel.Subagent : RoleLevel.Lookup;
    }

    /// <summary>A lookup agent gets its checks; any other session that names a tool is refused.</summary>
    private void CheckTool()
    {
        if (_level == RoleLevel.Lookup)
        {
            new LookupChecks(request.Tags, _tool, _role, sessionOf).Run();
            return;
        }

        if (_tool is null) return;
        var notALookup = new ToolOnlyForLookupsHoles(_role ?? "the default voice");
        throw new ToolsetException(MessageText.Format(ApiErrorMessage.ToolOnlyForLookups, notALookup));
    }

    /// <summary>
    /// The toolsets: a lookup agent's is the one that builds its tool (AskAgent sends none for it); anyone else's what
    /// it asked for, or, asked for none, its default. Then the role's own (F33, from its header's metadata, CL27), and
    /// every name, optional or not, held to the same rules.
    /// </summary>
    private void Toolsets()
    {
        IEnumerable<string> asked = _level == RoleLevel.Lookup && request.Toolsets is null
            ? [ToolsetOfTheTool()]
            : request.Toolsets ?? Unchosen();
        _toolsets = ToolsetCatalog.Normalise(ToolsetCatalog.Normalise(asked)
            .Concat(SkillLoader.ToolsetsOf(_role, _level)));
    }

    /// <summary>The toolset that builds a lookup agent's tool, or the refusal that none does.</summary>
    private string ToolsetOfTheTool()
    {
        if (engines.Catalog.ToolsetOf(_tool!) is { } toolset) return toolset;
        var noToolset = new NoToolsetForToolHoles(_tool!);
        throw new ToolsetException(MessageText.Format(ApiErrorMessage.NoToolsetForTool, noToolset));
    }

    /// <summary>
    /// The toolsets of a session nobody chose any for: an identity's voice asked by another agent gets its own packs,
    /// as a visitor at the door does; every other session the instance's default.
    /// </summary>
    private IEnumerable<string> Unchosen() =>
        _who is not null && _level == RoleLevel.Agent && request.Depth > 0
            ? engines.Catalog.ToolsetsOf(_who)
            : AgentSessions.DefaultToolsets;

    /// <summary>A lookup agent asks no one, so it may never load the toolset that asks.</summary>
    private void CheckLookupAsksNoOne()
    {
        if (_level != RoleLevel.Lookup ||
            !_toolsets.Select(ToolsetCatalog.NameOf).Contains(AgentSessions.AgentsToolset))
            return;
        var asks = new LookupCannotAskHoles(AgentSessions.AgentsToolset);
        throw new ToolsetException(MessageText.Format(ApiErrorMessage.LookupCannotAsk, asks));
    }

    /// <summary>Who a session in this identity runs as; none for a session of no identity.</summary>
    private TurnIdentity? RunsAs() =>
        (_who ?? engines.Catalog.IdentityOf(_role)) is { } identity ? TurnIdentity.Of(identity) : null;

    /// <summary>
    /// The chain of asks the session opens in (F127.5): its parent's, when this instance has the parent, then its own
    /// agent. An agent already in it is refused, so the asks can't go round in a loop.
    /// </summary>
    /// <remarks>Guard clauses (Fowler): no agent to add, one already there, then the chain one longer.</remarks>
    private IReadOnlyList<string> Chain(string? agent)
    {
        string? parentId = request.Tags?.Parent?.Split('#')[0];
        IReadOnlyList<string> above = parentId is null ? [] : sessionOf(parentId)?.Chain ?? [];
        if (agent is null) return above;
        if (!above.Contains(agent)) return [.. above, agent];
        var inChain = new AgentAlreadyInChainHoles(agent, string.Join(" > ", above));
        throw new RoleException(MessageText.Format(ApiErrorMessage.AgentAlreadyInChain, inChain));
    }

    /// <summary>
    /// The engine for what the session loads: an optional toolset this instance lacks is left out, and the session
    /// says so.
    /// </summary>
    private Core.Agent.AgentEngine Engine(string? llm)
    {
        _toolsets = engines.Catalog.Resolve(_toolsets, engines.Context).Select(t => t.Name).ToList();
        return engines.GetOrCreate(_toolsets, _role, _level, llm, _tool);
    }
}
