using MongoDB.Bson.Serialization.Attributes;

namespace LouisAgent.Api.Evals.Calibrations.Store;

/// <summary>
/// A judge check as MongoDB keeps it in the <c>calibrations</c> collection: which run and grader, the judge model,
/// how many agreed against the bar, each class's counts and every disagreement. Reports are added, never updated, so
/// it has no id of its own: Mongo gives it one on insert. A field added later never breaks a read.
/// </summary>
/// <remarks>
/// Metadata Mapping on a Data Transfer Object (Fowler, PoEAA): the record says how each field is stored and the
/// driver does the copying, as the eval store's <c>RunDocument</c> does. The properties keep the order the hand
/// mapping wrote, so a document is the same field for field.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record CalibrationDocument(
    [property: BsonElement("run")] string Run,
    [property: BsonElement("grader")] string Grader,
    [property: BsonElement("judge_model")] string? JudgeModel,
    [property: BsonElement("at")] DateTimeOffset At,
    [property: BsonElement("agreed")] int Agreed,
    [property: BsonElement("total")] int Total,
    [property: BsonElement("bar")] int Bar,
    [property: BsonElement("passes")] bool Passes,
    [property: BsonElement("person_pass")] ClassCountsDocument PersonPass,
    [property: BsonElement("person_fail")] ClassCountsDocument PersonFail,
    [property: BsonElement("disagreements")]
    IReadOnlyList<DisagreementDocument> Disagreements)
{
    /// <summary>The document for a judge check's report.</summary>
    public static CalibrationDocument Of(CalibrationRecord r) =>
        new(r.Run, r.Grader, r.JudgeModel, r.At, r.Agreed, r.Total, r.Bar, r.Passes,
            ClassCountsDocument.Of(r.PersonPass), ClassCountsDocument.Of(r.PersonFail),
            [.. r.Disagreements.Select(DisagreementDocument.Of)]);

    /// <summary>The report this document holds.</summary>
    public CalibrationRecord ToRecord() =>
        new(Run, Grader, JudgeModel, At, Agreed, Total, Bar, Passes, PersonPass.ToCounts(), PersonFail.ToCounts(),
            [.. Disagreements.Select(d => d.ToDisagreement())]);
}

/// <summary>How the judge answered on the conversations a person put in one class.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record ClassCountsDocument(
    [property: BsonElement("judge_pass")] int JudgePass,
    [property: BsonElement("judge_fail")] int JudgeFail,
    [property: BsonElement("judge_unknown")]
    int JudgeUnknown)
{
    /// <summary>The document for a class's counts.</summary>
    public static ClassCountsDocument Of(CalibrationClassCounts c) => new(c.JudgePass, c.JudgeFail, c.JudgeUnknown);

    /// <summary>The class's counts this document holds.</summary>
    public CalibrationClassCounts ToCounts() => new(JudgePass, JudgeFail, JudgeUnknown);
}

/// <summary>One conversation where the judge and the person disagreed, with the judge's reason.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record DisagreementDocument(
    [property: BsonElement("scenario")] string Scenario,
    [property: BsonElement("mode")] string Mode,
    [property: BsonElement("repeat")] int Repeat,
    [property: BsonElement("label")] string Label,
    [property: BsonElement("judge")] string Judge,
    [property: BsonElement("reason")] string Reason)
{
    /// <summary>The document for a disagreement.</summary>
    public static DisagreementDocument Of(CalibrationDisagreement d) =>
        new(d.Scenario, d.Mode, d.Repeat, d.Label, d.Judge, d.Reason);

    /// <summary>The disagreement this document holds.</summary>
    public CalibrationDisagreement ToDisagreement() => new(Scenario, Mode, Repeat, Label, Judge, Reason);
}
