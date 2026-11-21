namespace LouisAgent.Core.Hosting.SessionLlms;

/// <summary>A session named an LLM profile this instance doesn't offer, or a listed profile can't be read (F30).</summary>
public sealed class SessionLlmException(string message) : Exception(message);
