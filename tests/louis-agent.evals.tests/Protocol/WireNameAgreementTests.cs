using System.Text.Json;
using System.Text.Json.Serialization;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Choices;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Tests.Protocol;

/// <summary>
/// The protocol's enums travel as snake_case text, and two things write it: SettingChoice and the JSON writer. Both
/// must write the same word for every member, so a two-word member that forgot its wire name (`apierror` against
/// `api_error`) fails here and not in a stored run. A grader kind and a model level travel as their member name
/// (`FixedRule`), as they always have, and SettingChoice reads that too.
/// </summary>
public class WireNameAgreementTests
{
    /// <summary>The JSON writer the way the protocol uses it: enums as snake_case text.</summary>
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };

    /// <summary>The word the JSON writer puts on the wire for this member.</summary>
    private static string Written<T>(T member) where T : struct, Enum =>
        JsonSerializer.Deserialize<string>(JsonSerializer.Serialize(member, Options))!;

    /// <summary>For every enum on the wire, SettingChoice and the JSON writer spell each member the same.</summary>
    [Test]
    public void WhatSettingChoiceWrites_IsWhatTheJsonWriterWrites_ForEveryProtocolEnum()
    {
        foreach (var m in Enum.GetValues<TrialFailure>())
            Assert.That(m.Wire(), Is.EqualTo(Written(m)), $"TrialFailure.{m}");
        foreach (var m in Enum.GetValues<EvalTurnFailure>())
            Assert.That(m.Wire(), Is.EqualTo(Written(m)), $"BenchmarkTurnFailure.{m}");
        foreach (var m in Enum.GetValues<EvalRunSplit>())
            Assert.That(m.Wire(), Is.EqualTo(Written(m)), $"BenchmarkRunSplit.{m}");
    }

    /// <summary>
    /// The harness writes a turn's failure and the API reads and stores it, so both name the same failures in the
    /// same words. A failure added to one side only fails here, not in a stored run.
    /// </summary>
    [Test]
    public void TheHarnessAndTheApiNameTheSameFailures_InTheSameWords() =>
        Assert.That(Enum.GetValues<EvalTurnFailure>().Select(m => m.Wire()).Order(),
            Is.EqualTo(Enum.GetValues<TrialFailure>().Select(m => m.Wire()).Order()));
}
