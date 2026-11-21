namespace LouisAgent.Core.Agent.Toolsets;

/// <summary>A toolset request that can't be honoured: an unknown name, or a toolset whose settings are missing.</summary>
public sealed class ToolsetException(string message) : Exception(message);
