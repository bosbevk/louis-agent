
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Agent.EngineTools.Scripts;
/// <summary>A typed parameter of an agent-built tool.</summary>
public sealed record ScriptToolParameter(string Name, string Type, bool Required, string Description);
