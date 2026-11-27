namespace LouisAgent.Core.Config.Messages.Holes;

/// <summary>What a TOOL_SEARCH it doesn't know is refused with: the setting, the word and the modes it takes.</summary>
public sealed record UnknownToolSearchHoles(string Setting, string Value, string Supported);
