namespace LouisAgent.Core.Llm;

using Anthropic.Models.Messages;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Usage;

/// <summary>
/// Turns a provider adapter's <see cref="UsageDetails"/> into the ledger's four non-overlapping token kinds. One per
/// provider, chosen by <c>LlmClientFactory.CreateUsageMapper</c>, so provider differences stay out of the recorder.
/// </summary>
public interface IUsageMapper
{
    /// <summary>Maps one request's usage. Null usage, or a count the provider didn't report, maps to null, never 0.</summary>
    UsageTokens Map(UsageDetails? usage);

    /// <summary>
    /// Whether the recorder should keep this raw object (the provider's own response, or one of its stream events) for
    /// <see cref="Map(UsageDetails?, IReadOnlyList{object})"/>: for counts <see cref="UsageDetails"/> has no field for.
    /// </summary>
    bool Keeps(object raw);

    /// <summary>Maps one request's usage with the raw objects <see cref="Keeps"/> chose, in the order they came.</summary>
    UsageTokens Map(UsageDetails? usage, IReadOnlyList<object> raw);
}
