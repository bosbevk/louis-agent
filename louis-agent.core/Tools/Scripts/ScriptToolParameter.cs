namespace LouisAgent.Core.Tools.Scripts;

using Microsoft.Extensions.AI;

/// <summary>A typed parameter of an agent-built tool.</summary>
public sealed record ScriptToolParameter(string Name, string Type, bool Required, string Description);
