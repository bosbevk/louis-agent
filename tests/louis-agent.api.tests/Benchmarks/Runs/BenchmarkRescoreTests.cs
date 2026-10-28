using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Api.Benchmarks.Export;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Tests.Benchmarks.Runs;

/// <summary>
/// A stored run is re-scored by the eval runner, which gets the run's export on its stdin and the run's id in an
/// argument list. The API reads, stores, starts and shows; it grades nothing.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
public class BenchmarkRescoreTests
{
    /// <summary>A re-score's first line names the run it re-scored; an ordinary run's names none.</summary>
    [Test]
    public void TheBeginLineNamesTheRunItRescored_AndAnOldRunNamesNone()
    {
        var rescored =
            (ProtocolStarted)Parse(
                """@{"event":"protocol","version":1,"eval":"stub","eval_version":1,"rescore_of":"old123456789","graders":[],"models":[]}""");
        var ordinary =
            (ProtocolStarted)Parse(
                """@{"event":"protocol","version":1,"eval":"stub","eval_version":1,"graders":[],"models":[]}""");

        Assert.That(rescored.RescoreOf, Is.EqualTo("old123456789"));
        Assert.That(ordinary.RescoreOf, Is.Null);
    }

    /// <summary>The export writes back which run a re-score came from, and nothing of it for an ordinary run.</summary>
    [Test]
    public async Task TheExportWritesRescoreOfBack_AndAnOrdinaryRunWritesNone()
    {
        var store = new InMemoryBenchmarkStore();
        await store.SaveRunAsync(new BenchmarkDocuments.Run("a", "stub", 1, BenchmarkRunStatus.Done,
            DateTimeOffset.UnixEpoch, Eval: "stub", RescoreOf: "old123456789"));
        await store.SaveRunAsync(new BenchmarkDocuments.Run("b", "stub", 1, BenchmarkRunStatus.Done,
            DateTimeOffset.UnixEpoch, Eval: "stub"));

        string rescored =
            await BenchmarkProtocolExport.BuildAsync(store, (await store.GetRunAsync("a"))!, CancellationToken.None);
        string ordinary =
            await BenchmarkProtocolExport.BuildAsync(store, (await store.GetRunAsync("b"))!, CancellationToken.None);

        Assert.That(((ProtocolStarted)Parse(rescored.Split('\n')[0])).RescoreOf, Is.EqualTo("old123456789"));
        Assert.That(ordinary, Does.Not.Contain("rescore_of"));
    }

    /// <summary>
    /// The pump writes the whole export to the runner and closes its input; a runner that's already gone is no
    /// error.
    /// </summary>
    [Test]
    public async Task ThePump_WritesEverythingAndCloses_AndToleratesARunnerThatHasGone()
    {
        var writer = new RecordingWriter();
        await BenchmarkRunner.PumpAsync(writer, "@{}\n");
        Assert.That((writer.Text, writer.Closed), Is.EqualTo(("@{}\n", true)));

        Assert.That(async () => await BenchmarkRunner.PumpAsync(new RecordingWriter(throwOnWrite: true), "@{}\n"),
            Throws.Nothing, "a runner that ended without reading is not an error here");
    }

    /// <summary>
    /// Stands in for the runner's stdin: keeps what was written and whether it was closed, or acts like a runner that
    /// has gone.
    /// </summary>
    private sealed class RecordingWriter(bool throwOnWrite = false) : StringWriter
    {
        /// <summary>Whether the pump closed it.</summary>
        public bool Closed { get; private set; }

        /// <summary>Everything written so far.</summary>
        public string Text => ToString();

        /// <summary>Writes like any string writer, or fails as a broken pipe when the runner has gone.</summary>
        public override Task WriteAsync(string? value) =>
            throwOnWrite ? throw new IOException("Broken pipe") : base.WriteAsync(value);

        /// <summary>Notes that it was closed, then closes.</summary>
        public override void Close()
        {
            Closed = true;
            base.Close();
        }
    }
}
