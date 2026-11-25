using LouisAgent.Composition.Messages;
using LouisAgent.Core.Agent.Turns;
using LouisAgent.Core.Hosting.Messages;
using LouisAgent.Core.Hosting.Messages.Holes;
using LouisAgent.Core.Llm.UsageMapping;
using LouisAgent.Core.Sessions.Replay.Store;
using LouisAgent.Core.Usage.Sinks;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Hosting.SessionLlms;

/// <summary>
/// The models a pack's tool may be given (F171.1): the instance's own, and one for each profile in
/// <c>LLM_SESSION_PROFILES</c>, each wrapped once as the summaries' model is, traced, answered from an eval's
/// recordings when a trial replays, and written to the ledger as the tool's. The host builds them once and hands
/// them to the program's catalogue, which picks one for each pack.
/// </summary>
/// <remarks>
/// The catalogue hands each pack its model, chosen over the pack context carrying one and over a function's
/// services, and picks it per pack in code, chosen over one setting for all (Louis, 2026-11-23). A pack sees only
/// Microsoft's <see cref="IChatClient" />, so a tool that reads a file works the same outside our framework
/// (ADR 0028). A name that isn't a profile stops the host at start-up, never at a visitor's first upload.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0028-packs-stay-convertible-to-mcp.md">ADR 0028: packs stay convertible to MCP</seealso>
public sealed class ToolModels
{
    private readonly IReadOnlyDictionary<string, IChatClient> _profiles;

    private ToolModels(IChatClient instance, IReadOnlyDictionary<string, IChatClient> profiles)
    {
        Instance = instance;
        _profiles = profiles;
    }

    /// <summary>The instance's own model.</summary>
    public IChatClient Instance { get; }

    /// <summary>
    /// The model of the profile with this name; one that isn't a profile throws, naming the ones there are.
    /// </summary>
    public IChatClient Named(string profile)
    {
        if (_profiles.TryGetValue(profile, out var model)) return model;
        var unknown = new UnknownSessionLlmHoles(profile,
            _profiles.Count == 0 ? "none" : string.Join(", ", _profiles.Keys));
        throw new SessionLlmException(MessageText.Format(HostingErrorMessage.UnknownSessionLlm, unknown));
    }

    /// <summary>Wraps the instance's model and each profile's once, each with its own usage mapping.</summary>
    internal static ToolModels Create(IChatClient instance, IUsageMapper? instanceMapper,
        IReadOnlyList<SessionLlm> profiles, IUsageSink? usageSink, ReplayStore? replay = null)
    {
        var wrapped = profiles.ToDictionary(profile => profile.Name,
            profile => ChatPipeline.BuildToolClient(profile.Client, usageSink, profile.UsageMapper, replay),
            StringComparer.Ordinal);
        return new ToolModels(ChatPipeline.BuildToolClient(instance, usageSink, instanceMapper, replay), wrapped);
    }
}
