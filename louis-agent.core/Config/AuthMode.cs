namespace LouisAgent.Core.Config;

/// <summary>AGENT_AUTH (FX18): whether a host that serves over the network may start without a key.</summary>
public enum AuthMode
{
    /// <summary>The default: without a key the host refuses to start.</summary>
    Required,

    /// <summary>The host starts without a key, on purpose, and says so; a key that is set is still checked.</summary>
    Off,
}
