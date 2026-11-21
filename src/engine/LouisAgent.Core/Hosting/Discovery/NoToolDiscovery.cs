using LouisAgent.Core.Agent;

namespace LouisAgent.Core.Hosting.Discovery;

/// <summary>The discovery a host gets when it hands in none: it looks nowhere and registers nothing.</summary>
/// <remarks>Null Object, so <see cref="AgentHost"/> starts whatever it was given without a check.</remarks>
public sealed class NoToolDiscovery : IToolDiscovery
{
    /// <summary>The one instance: it holds nothing of its own.</summary>
    public static readonly NoToolDiscovery Instance = new();

    private NoToolDiscovery()
    {
    }

    /// <summary>Nothing to look for.</summary>
    public void Start(AgentEngine engine)
    {
    }
}
