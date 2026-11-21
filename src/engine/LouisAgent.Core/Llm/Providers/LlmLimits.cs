namespace LouisAgent.Core.Llm.Providers;

/// <summary>What the engine asks of every model on every request, so a provider's client and the turn agree.</summary>
public static class LlmLimits
{
    /// <summary>
    /// Output limit per reply, thinking included: room for a sizeable file in one tool call; larger files are
    /// written in chunks (see default.md).
    /// </summary>
    public const int MaxOutputTokens = 16_000;
}
