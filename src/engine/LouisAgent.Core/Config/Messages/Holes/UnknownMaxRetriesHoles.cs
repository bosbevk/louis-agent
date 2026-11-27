namespace LouisAgent.Core.Config.Messages.Holes;

/// <summary>
/// What an LLM_MAX_RETRIES that isn't a count is refused with (F8): the word, and the default when unset.
/// </summary>
public sealed record UnknownMaxRetriesHoles(string Value, int Default);
