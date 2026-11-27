namespace LouisAgent.Core.Agent.EngineTools.ToolSearch;

/// <summary>
/// What <c>FindTool</c> answers (F97.3): the names of the tools that fit best, best first. A provider that loads tools
/// on demand turns them into its own references; on a replay the same names come back.
/// </summary>
public sealed record FoundTools(IReadOnlyList<string> Names);
