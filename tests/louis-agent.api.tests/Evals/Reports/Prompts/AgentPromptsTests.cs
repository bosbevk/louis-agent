using System.Text.Json;
using LouisAgent.Api.Evals.Reports.Prompts;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Recording;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Sinks;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Tests.Evals.Reports.Prompts;

/// <summary>
/// A run keeps which prompts each of its agents used, per mode, so the trend can tell a changed prompt from an agent
/// that simply wasn't called this time.
/// </summary>
public class AgentPromptsTests
{
    /// <summary>The id of the run the rows belong to.</summary>
    private const string Run = "run123456789";

    /// <summary>One ledger row of the run: who asked (its task), in which mode, why, and with which prompt.</summary>
    private static UsageRecord Row(string? task, string? hash, string mode = SampleMode.Beta,
        string purpose = UsagePurpose.Turn, string? tag = null) =>
        new(DateTimeOffset.UtcNow, 1, purpose, "api", "s", 1, task, tag ?? $"{Run}:{mode}:1", null, "m",
            new UsageTokens(10, 0, 0, 1, null), 100, "stop", new UsageCost("USD", 0.01m, "2026-10-10"),
            PromptHash: hash);

    /// <summary>The main session, a specialist and a minion each get their own hashes, in each mode.</summary>
    [Test]
    public void EachAgent_GetsItsOwnHashes_PerMode()
    {
        UsageRecord[] rows =
        [
            Row("bobotie", "a"), Row("sub-session:puls-cook", "c"), Row("minion:pricer", "m"),
            Row("bobotie", "b", mode: SampleMode.Alpha),
        ];

        var prompts = AgentPrompts.Of(Run, rows);

        Assert.That(prompts,
            Is.EqualTo(new[]
            {
                new AgentPrompt(SampleMode.Alpha, AgentPrompts.Main, ["b"]),
                new AgentPrompt(SampleMode.Beta, AgentPrompts.Main, ["a"]),
                new AgentPrompt(SampleMode.Beta, "minion:pricer", ["m"]),
                new AgentPrompt(SampleMode.Beta, "sub-session:puls-cook", ["c"]),
            }).Using<AgentPrompt>(Same));
    }

    /// <summary>Every scenario's main session is the same agent, and its hashes are listed once each, sorted.</summary>
    [Test]
    public void AnAgentsHashes_AreDistinctAndSorted_WhateverTheScenario()
    {
        UsageRecord[] rows = [Row("bobotie", "b"), Row("cottage-pie", "a"), Row(null, "b")];

        var prompts = AgentPrompts.Of(Run, rows)!;

        Assert.That(prompts.Single().Hashes, Is.EqualTo(new[] { "a", "b" }));
    }

    /// <summary>A judge and a summary ask with prompts of their own, so they never mix into the agent's.</summary>
    [Test]
    public void AJudgeAndASummary_AreNamedApart_FromTheAgentTheyServe()
    {
        UsageRecord[] rows =
        [
            Row("bobotie", "a"), Row("bobotie", "a", purpose: UsagePurpose.Continue),
            Row("bobotie", "j", purpose: UsagePurpose.Grading),
            Row("sub-session:puls-cook", "s", purpose: UsagePurpose.Summary),
        ];

        var agents = AgentPrompts.Of(Run, rows)!.Select(p => (p.Agent, string.Join(",", p.Hashes)));

        Assert.That(agents,
            Is.EquivalentTo(new[]
            {
                (AgentPrompts.Main, "a"), ($"{AgentPrompts.Main} (grading)", "j"),
                ("sub-session:puls-cook (summary)", "s"),
            }));
    }

    /// <summary>A row tagged with the run alone has no mode, and is still kept.</summary>
    [Test]
    public void ARowTaggedWithTheRunAlone_HasNoMode()
    {
        var prompts = AgentPrompts.Of(Run, [Row("bobotie", "j", purpose: UsagePurpose.Grading, tag: Run)])!;

        Assert.That(prompts.Single().Mode, Is.Null);
    }

    /// <summary>A ledger from before prompts were hashed tells us nothing: unknown, never an empty list.</summary>
    [Test]
    public void ALedgerWithNoHashes_IsUnknown_AndRowsWithoutOneAreSkipped()
    {
        Assert.That(AgentPrompts.Of(Run, [Row("bobotie", null), Row("sub-session:puls-cook", null)]), Is.Null);
        Assert.That(AgentPrompts.Of(Run, [Row("bobotie", null), Row("sub-session:puls-cook", "c")])!
            .Select(p => p.Agent), Is.EqualTo(new[] { "sub-session:puls-cook" }));
    }

    /// <summary>When a run ends, the recorder keeps each agent's prompts on it, read from the ledger.</summary>
    [Test]
    public async Task ARecordedRun_KeepsItsAgentsPrompts()
    {
        string directory = Path.Combine(Path.GetTempPath(), "louis-ledger-" + Guid.NewGuid().ToString("N"));
        // The folder is ours alone, and the finally removes it whatever the test does.
        try
        {
            // The ledger holds a main session's row and its cook's.
            var sink = new JsonlUsageSink(directory);
            sink.Record(Row("bobotie", "a"));
            sink.Record(Row("sub-session:puls-cook", "c"));

            // A run that starts and ends at once; the recorder joins the ledger when it ends.
            var store = new InMemoryEvalStore();
            var run = new RunningEval(Run, "cook", 1);
            var recording = new EvalRecorder(store, ledgerDirectory: directory).RecordAsync(run);
            run.Add("@" + JsonSerializer.Serialize(new ProtocolLine("protocol", 1)));
            run.Complete(0);
            await recording;

            Assert.That((await store.GetRunAsync(Run))!.Prompts,
                Is.EqualTo(new[]
                {
                    new AgentPrompt(SampleMode.Beta, AgentPrompts.Main, ["a"]),
                    new AgentPrompt(SampleMode.Beta, "sub-session:puls-cook", ["c"]),
                }).Using<AgentPrompt>(Same));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>The runner's first line, as the recorder reads it.</summary>
    private sealed record ProtocolLine(
        [property: System.Text.Json.Serialization.JsonPropertyName("event")]
        string Event,
        [property: System.Text.Json.Serialization.JsonPropertyName("version")]
        int Version);

    /// <summary>
    /// Two entries say the same: a record holds its list by reference, so the hashes are compared by value.
    /// </summary>
    private static bool Same(AgentPrompt a, AgentPrompt b) =>
        a.Mode == b.Mode && a.Agent == b.Agent && a.Hashes.SequenceEqual(b.Hashes);
}
