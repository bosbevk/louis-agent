using System.Text.Json;
using System.Text.Json.Serialization;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Choices;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Evals.Tests.Protocol;

/// <summary>
/// The protocol's enums travel as text, and two things write it: SettingChoice and the JSON writer. Both must write
/// the same word for every member, so a two-word member that forgot its wire name fails here and not in a stored run.
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
        foreach (var m in Enum.GetValues<TurnFailure>())
            Assert.That(m.Wire(), Is.EqualTo(Written(m)), $"TurnFailure.{m}");
        foreach (var m in Enum.GetValues<EvalTurnFailure>())
            Assert.That(m.Wire(), Is.EqualTo(Written(m)), $"BenchmarkTurnFailure.{m}");
        foreach (var m in Enum.GetValues<EvalRunSplit>())
            Assert.That(m.Wire(), Is.EqualTo(Written(m)), $"BenchmarkRunSplit.{m}");
    }

    /// <summary>
    /// The harness writes a turn's failure and the API reads and stores it, so both name the same failures in the
    /// same words: until step 7c.3 the API reads version 1's, which the harness still writes.
    /// </summary>
    [Test]
    public void TheHarnessAndTheApiNameTheSameFailures_InTheSameWords() =>
        Assert.That(Enum.GetValues<EvalTurnFailure>().Select(m => m.Wire()).Order(),
            Is.EqualTo(Enum.GetValues<TurnFailure>().Select(V1Words.Failure).Order()));
}
