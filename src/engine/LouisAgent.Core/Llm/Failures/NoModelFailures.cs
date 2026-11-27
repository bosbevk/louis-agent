namespace LouisAgent.Core.Llm.Failures;

/// <summary>
/// A provider that names none of its failures (F8): each one is "other", so nothing is asked again and every failure
/// is passed on as before.
/// </summary>
/// <remarks>Null Object, the default of <c>ILlmProvider.CreateFailures</c>, as <c>NoPromptCaching</c> is.</remarks>
public sealed class NoModelFailures : IModelFailures
{
    /// <summary>The one instance; it holds nothing.</summary>
    public static NoModelFailures Instance { get; } = new();

    private NoModelFailures()
    {
    }

    /// <inheritdoc />
    public ModelFailure Of(Exception failure) => ModelFailure.Other;
}
