using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Api.Benchmarks.Export;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Tests.Benchmarks.Runs;

// F18 step 5b.2: a stored run is re-scored by the eval runner, which gets the run's export on stdin and `--rescore {id}` as an argument list (ADR 0004). The API
// reads, stores, starts and shows; it grades nothing. The end-to-end test runs the real runner on the C# stub, which opens no session and costs nothing.
// Written first: BenchmarkRunner had no StartRescore and the protocol's first line no rescore_of.
public class BenchmarkRescoreTests
{
    [Test]
    public void TheBeginLineNamesTheRunItRescored_AndAnOldRunNamesNone()
    {
        var rescored = (ProtocolStarted)Parse("""@{"event":"protocol","version":1,"eval":"stub","eval_version":1,"rescore_of":"old123456789","graders":[],"models":[]}""");
        var ordinary = (ProtocolStarted)Parse("""@{"event":"protocol","version":1,"eval":"stub","eval_version":1,"graders":[],"models":[]}""");

        Assert.That(rescored.RescoreOf, Is.EqualTo("old123456789"));
        Assert.That(ordinary.RescoreOf, Is.Null);
    }

    [Test]
    public async Task TheExportWritesRescoreOfBack_AndAnOrdinaryRunWritesNone()
    {
        var store = new InMemoryBenchmarkStore();
        await store.SaveRunAsync(new BenchmarkDocuments.Run("a", "stub", 1, BenchmarkRunStatus.Done, DateTimeOffset.UnixEpoch, Eval: "stub", RescoreOf: "old123456789"));
        await store.SaveRunAsync(new BenchmarkDocuments.Run("b", "stub", 1, BenchmarkRunStatus.Done, DateTimeOffset.UnixEpoch, Eval: "stub"));

        string rescored = await BenchmarkProtocolExport.BuildAsync(store, (await store.GetRunAsync("a"))!, CancellationToken.None);
        string ordinary = await BenchmarkProtocolExport.BuildAsync(store, (await store.GetRunAsync("b"))!, CancellationToken.None);

        Assert.That(((ProtocolStarted)Parse(rescored.Split('\n')[0])).RescoreOf, Is.EqualTo("old123456789"));
        Assert.That(ordinary, Does.Not.Contain("rescore_of"));
    }

    [Test]
    public async Task ThePump_WritesEverythingAndCloses_AndToleratesARunnerThatHasGone()
    {
        var writer = new RecordingWriter();
        await BenchmarkRunner.PumpAsync(writer, "@{}\n");
        Assert.That((writer.Text, writer.Closed), Is.EqualTo(("@{}\n", true)));

        Assert.That(async () => await BenchmarkRunner.PumpAsync(new RecordingWriter(throwOnWrite: true), "@{}\n"), Throws.Nothing, "a runner that ended without reading is not an error here");
    }

    private sealed class RecordingWriter(bool throwOnWrite = false) : StringWriter
    {
        public bool Closed { get; private set; }

        public string Text => ToString();

        public override Task WriteAsync(string? value) => throwOnWrite ? throw new IOException("Broken pipe") : base.WriteAsync(value);

        public override void Close()
        {
            Closed = true;
            base.Close();
        }
    }
}
