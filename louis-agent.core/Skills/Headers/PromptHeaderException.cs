namespace LouisAgent.Core.Skills.Headers;

/// <summary>
/// A prompt's header (CL27) says something the reader doesn't take: the message names the file and what to change.
/// </summary>
public sealed class PromptHeaderException(string message) : Exception(message);
