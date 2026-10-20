namespace LouisAgent.Core.Hosting.SessionLlms;

using LouisAgent.Core.Llm.UsageMapping;
using LouisAgent.Core.Llm;
using Microsoft.Extensions.AI;

/// <summary>
/// A model a session may name when it opens (F30): an LLM profile from <c>LLM_SESSION_PROFILES</c>, with the client,
/// usage mapping, tool support and thinking its provider needs, built by the same <see cref="ILlmClientFactory"/> as
/// the instance's own model.
/// </summary>
/// <param name="Name">The profile's name, as in <c>config/.env.{Name}</c> and a session's <c>llm</c>.</param>
public sealed record SessionLlm(string Name, IChatClient Client, bool SupportsTools = true, IUsageMapper? UsageMapper = null,
    ReasoningEffort? Thinking = null);
