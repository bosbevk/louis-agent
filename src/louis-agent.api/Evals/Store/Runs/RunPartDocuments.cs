using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Run;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Evals.Store.Runs;

/// <summary>
/// One repeat of a mode inside a run's document: what its sessions opened with, how many conversations it planned,
/// when it started and ended, and its pass rates once it ended.
/// </summary>
/// <remarks>Metadata Mapping on a Data Transfer Object, as <see cref="RunDocument"/> explains.</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record ModeDocument(
    [property: BsonElement("name")] string Name,
    [property: BsonElement("repeat")] int Repeat,
    [property: BsonElement("toolsets")] IReadOnlyList<string> Toolsets,
    [property: BsonElement("role")] string? Role,
    [property: BsonElement("llm")] string? Llm,
    [property: BsonElement("conversations")]
    int? Conversations,
    [property: BsonElement("at")] DateTimeOffset? At,
    [property: BsonElement("done_at")] DateTimeOffset? DoneAt,
    [property: BsonElement("rates")] RatesDocument? Rates)
{
    /// <summary>The document for a mode's repeat.</summary>
    public static ModeDocument Of(Mode m) =>
        new(m.Name, m.Repeat, m.Toolsets, m.Role, m.Llm, m.Conversations, m.At, m.DoneAt,
            m.Rates is null ? null : RatesDocument.Of(m.Rates));

    /// <summary>The mode's repeat this document holds.</summary>
    public Mode ToMode() => new(Name, Repeat, Toolsets, Role, Llm, Rates?.ToRates(), Conversations, At, DoneAt);
}

/// <summary>A mode's pass rates as the runner wrote them; a rate no trial made stays null, never 0.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record RatesDocument(
    [property: BsonElement("pass_at_k")] double? PassAtK,
    [property: BsonElement("pass_hat_k")] double? PassHatK,
    [property: BsonElement("k")] int K,
    [property: BsonElement("infrastructure")]
    int Infrastructure,
    [property: BsonElement("retried")] int Retried)
{
    /// <summary>The document for a mode's rates.</summary>
    public static RatesDocument Of(ModeRates r) => new(r.PassAtK, r.PassHatK, r.K, r.Infrastructure, r.Retried);

    /// <summary>The rates this document holds.</summary>
    public ModeRates ToRates() => new(PassAtK, PassHatK, K, Infrastructure, Retried);
}

/// <summary>What a run cost from the ledger; an unknown price keeps the cost null, never 0.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record TotalDocument(
    [property: BsonElement("cost"), BsonRepresentation(BsonType.Decimal128)]
    decimal? Cost,
    [property: BsonElement("currency")] string Currency,
    [property: BsonElement("tokens")] long Tokens,
    [property: BsonElement("requests")] int Requests)
{
    /// <summary>The document for a run's total.</summary>
    public static TotalDocument Of(Total t) => new(t.Cost, t.Currency, t.Tokens, t.Requests);

    /// <summary>The total this document holds.</summary>
    public Total ToTotal() => new(Cost, Currency, Tokens, Requests);
}

/// <summary>One of the eval's graders, as the run started; a human review carries its question.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record GraderDocument(
    [property: BsonElement("name")] string Name,
    [property: BsonElement("version")] int Version,
    [property: BsonElement("kind")] GraderKind Kind,
    [property: BsonElement("question")] string? Question)
{
    /// <summary>The document for a grader.</summary>
    public static GraderDocument Of(Grader g) => new(g.Name, g.Version, g.Kind, g.Question);

    /// <summary>The grader this document holds.</summary>
    public Grader ToGrader() => new(Name, Version, Kind, Question);
}

/// <summary>Which model one level of a mode ran on.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record ModelDocument(
    [property: BsonElement("mode")] string Mode,
    [property: BsonElement("level")] ModelLevel Level,
    [property: BsonElement("role")] string? Role,
    [property: BsonElement("model")] string? Model)
{
    /// <summary>The document for a model's use.</summary>
    public static ModelDocument Of(ModelUse m) => new(m.Mode, m.Level, m.Role, m.Model);

    /// <summary>The model's use this document holds.</summary>
    public ModelUse ToModelUse() => new(Mode, Level, Role, Model);
}

/// <summary>Which prompts one agent used in one mode, as their hashes; a grading agent has no mode.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record PromptDocument(
    [property: BsonElement("mode")] string? Mode,
    [property: BsonElement("agent")] string Agent,
    [property: BsonElement("hashes")] IReadOnlyList<string> Hashes)
{
    /// <summary>The document for an agent's prompts.</summary>
    public static PromptDocument Of(AgentPrompt p) => new(p.Mode, p.Agent, p.Hashes);

    /// <summary>The agent's prompts this document holds.</summary>
    public AgentPrompt ToPrompt() => new(Mode, Agent, Hashes);
}

/// <summary>The dataset a run's scenarios came from: its name, version and hash.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record DatasetDocument(
    [property: BsonElement("name")] string Name,
    [property: BsonElement("version")] int Version,
    [property: BsonElement("hash")] string Hash)
{
    /// <summary>The document for a dataset.</summary>
    public static DatasetDocument Of(DatasetInfo d) => new(d.Name, d.Version, d.Hash);

    /// <summary>The dataset this document holds.</summary>
    public DatasetInfo ToDataset() => new(Name, Version, Hash);
}
