namespace LouisAgent.Core.Llm.Failures;

/// <summary>
/// How one provider names its failures (F8): which are worth asking again, which are an account limit, and which are
/// neither. Each provider knows its own errors; core knows only the kinds.
/// </summary>
/// <remarks>
/// Strategy (Gang of Four), as ADR 0003 names it, beside the prompt cache's and the stale thinking's: what a failure
/// means differs by provider, so each says it through its <c>ILlmProvider</c>, and nothing else reads a provider's
/// errors. The default, <see cref="NoModelFailures"/>, names none, so a provider that says nothing behaves as before.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0003-provider-strategies.md">ADR 0003: provider strategies</seealso>
public interface IModelFailures
{
    /// <summary>What this failure means for the request that met it.</summary>
    ModelFailure Of(Exception failure);
}
