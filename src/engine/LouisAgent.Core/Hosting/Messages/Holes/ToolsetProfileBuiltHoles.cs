namespace LouisAgent.Core.Hosting.Messages.Holes;

/// <summary>
/// A toolset profile's engine as it was built: what it holds, how many tools, its prompt's length, and about how
/// many tokens of fixed prefix every request sends.
/// </summary>
public sealed record ToolsetProfileBuiltHoles(string Toolsets, int Tools, int PromptChars, int PrefixTokens);
