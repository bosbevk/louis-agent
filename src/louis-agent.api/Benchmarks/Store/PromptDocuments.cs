using MongoDB.Bson;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Benchmarks.Store;

/// <summary>
/// A run's prompts per agent as MongoDB keeps them: an array on the run's document, one entry per mode and agent. A
/// run stored before they were kept has no <c>prompts</c> and reads as unknown, so the trend never marks it.
/// </summary>
/// <remarks>
/// Embedded Value (PoEAA; the unattended run's finding 13): the per-agent hashes are a small summary of the run, so
/// they live inside its document, as its slices do in <see cref="SliceDocuments"/>, not in a collection of their own.
/// Kept out of <see cref="MongoBenchmarkStore"/>, which is past its size limit.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
internal static class PromptDocuments
{
    /// <summary>The prompts as an array of documents; none known is a null, never an empty array.</summary>
    public static BsonValue ToBson(IReadOnlyList<AgentPrompt>? prompts) =>
        prompts is null
            ? BsonNull.Value
            : new BsonArray(prompts.Select(p => new BsonDocument
            {
                ["mode"] = p.Mode is null ? BsonNull.Value : new BsonString(p.Mode),
                ["agent"] = p.Agent,
                ["hashes"] = new BsonArray(p.Hashes),
            }));

    /// <summary>The prompts read back; a run from before them, or a null, has none.</summary>
    public static IReadOnlyList<AgentPrompt>? Of(BsonValue value) =>
        value is not { IsBsonArray: true }
            ? null
            : value.AsBsonArray.Select(v => v.AsBsonDocument).Select(d => new AgentPrompt(
                d["mode"].IsBsonNull ? null : d["mode"].AsString,
                d["agent"].AsString,
                d["hashes"].AsBsonArray.Select(h => h.AsString).ToArray())).ToArray();
}
