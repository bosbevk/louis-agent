namespace LouisAgent.Core.Agent;

using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web;

/// <summary>What a toolset needs to build its tools for one engine.</summary>
public sealed record ToolsetContext(AgentOptions Options, IWebSearchProvider WebSearch);
