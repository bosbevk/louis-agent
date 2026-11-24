using System.Text.Json;
using LouisAgent.Core.Agent.Replay;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Testing.Replay;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Agent.Replay;

/// <summary>
/// A tool's answers recorded under the session's trial and played back when the model is (FX235). Each tool here
/// counts its runs: a replay with the model must never run it, and a replay with a live model always does.
/// </summary>
public class RecordedToolTests
{
    /// <summary>The trial the tests record into and replay from.</summary>
    private const string Trial = "r1:a:1/t1";

    /// <summary>A fresh replay folder for each test.</summary>
    private ReplayFolder _folder = null!;

    /// <summary>How many times the tool under test ran.</summary>
    private int _runs;

    /// <summary>Makes the test's replay folder and sets the count to nothing.</summary>
    [SetUp]
    public void SetUp()
    {
        _folder = new ReplayFolder();
        _runs = 0;
    }

    /// <summary>Deletes the test's replay folder.</summary>
    [TearDown]
    public void TearDown() => _folder.Dispose();

    /// <summary>A record a tool may answer with, as the packs' tools sometimes do.</summary>
    private sealed record Found(int Count, string First);

    /// <summary>A search tool that answers this text, counting its runs.</summary>
    private AIFunction Search(string answer) =>
        AIFunctionFactory.Create((string query) =>
        {
            _runs++;
            return answer;
        }, "SearchDocs");

    /// <summary>The tool under test: <paramref name="inner"/> recorded around, over the test's store.</summary>
    private AIFunction Recorded(AIFunction inner) => (AIFunction)RecordedTool.Around(inner, _folder.Store);

    /// <summary>The session's replay for the duration of a block.</summary>
    private static IDisposable Flow(WebReplayMode web, bool model) =>
        SessionReplay.Use(new SessionReplay(web, Trial, model));

    /// <summary>The arguments a model would send for a search of this query.</summary>
    private static AIFunctionArguments Query(string query) => new() { ["query"] = query };

    /// <summary>What a value reads as to the model: its JSON, whether the tool gave text or a JSON value.</summary>
    private static string Json(object? value) => JsonSerializer.Serialize(value, AIJsonUtilities.DefaultOptions);

    /// <summary>
    /// A live run records the answer and hands it on unchanged; a free replay answers from the recording, even when
    /// the tool would now answer something else, and never runs it.
    /// </summary>
    [Test]
    public async Task ARecordedAnswer_IsPlayedBackWithTheModel_AndTheToolIsNeverRun()
    {
        object? live;
        using (Flow(WebReplayMode.Record, model: false))
            live = await Recorded(Search("3 lines")).InvokeAsync(Query("payment"));

        object? replayed;
        using (Flow(WebReplayMode.Replay, model: true))
            replayed = await Recorded(Search("4 lines, the docs changed")).InvokeAsync(Query("payment"));

        Assert.Multiple(() =>
        {
            Assert.That(Json(live), Is.EqualTo(Json("3 lines")));
            Assert.That(Json(replayed), Is.EqualTo(Json("3 lines")));
            Assert.That(_runs, Is.EqualTo(1), "only the live run ran the tool");
        });
    }

    /// <summary>An answer that is a record comes back as the same JSON the live tool gave the model.</summary>
    [Test]
    public async Task ARecordAnswer_IsPlayedBackAsTheSameJson()
    {
        var tool = AIFunctionFactory.Create(() =>
        {
            _runs++;
            return new Found(3, "README.md");
        }, "CountDocs");

        object? live;
        using (Flow(WebReplayMode.Record, model: false))
            live = await Recorded(tool).InvokeAsync(new AIFunctionArguments());
        object? replayed;
        using (Flow(WebReplayMode.Replay, model: true))
            replayed = await Recorded(tool).InvokeAsync(new AIFunctionArguments());

        Assert.That(Json(replayed), Is.EqualTo(Json(live)));
        Assert.That(_runs, Is.EqualTo(1));
    }

    /// <summary>
    /// A call is keyed by compact JSON, with no line break in it, so a recording made on Linux answers on Windows:
    /// indented JSON breaks its lines with the system's own newline, and a key with arguments then missed.
    /// </summary>
    [Test]
    public async Task ACallsKey_HoldsNoLineBreak_SoItAnswersOnEverySystem()
    {
        using (Flow(WebReplayMode.Record, model: false))
            await Recorded(Search("3 lines")).InvokeAsync(Query("payment"));

        string index = File.ReadAllText(_folder.Trials.Single());
        string key = System.Text.Json.Nodes.JsonNode.Parse(index)!["key"]!.GetValue<string>();
        // The arguments sit in the key as a JSON string, so a line break in them reads there as an escape.
        Assert.That(key, Does.Not.Contain(@"\n").And.Not.Contain(@"\r"), key);
    }

    /// <summary>A tool that failed on the live run fails again on the replay, with the same message, unrun.</summary>
    [Test]
    public async Task AFailure_IsPlayedBackWithItsMessage()
    {
        var tool = AIFunctionFactory.Create((string query) =>
        {
            _runs++;
            throw new InvalidOperationException("The docs root is not set.");
#pragma warning disable CS0162 // A tool must say what it answers, even one that only fails.
            return "";
#pragma warning restore CS0162
        }, "SearchDocs");

        using (Flow(WebReplayMode.Record, model: false))
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await Recorded(tool).InvokeAsync(Query("payment")));

        using (Flow(WebReplayMode.Replay, model: true))
        {
            var replayed = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await Recorded(tool).InvokeAsync(Query("payment")));
            Assert.That(replayed!.Message, Is.EqualTo("The docs root is not set."));
        }

        Assert.That(_runs, Is.EqualTo(1));
    }

    /// <summary>A call the recording never saw, by its arguments too, is a named miss; the tool isn't run.</summary>
    [Test]
    public async Task ACallTheRecordingNeverSaw_IsAMiss_NamingTheTool()
    {
        using (Flow(WebReplayMode.Record, model: false))
            await Recorded(Search("3 lines")).InvokeAsync(Query("payment"));

        using (Flow(WebReplayMode.Replay, model: true))
        {
            var miss = Assert.ThrowsAsync<ToolReplayMissException>(async () =>
                await Recorded(Search("3 lines")).InvokeAsync(Query("pricing")));
            Assert.That(miss!.Message, Does.Contain("SearchDocs").And.Contain(Trial));
        }

        Assert.That(_runs, Is.EqualTo(1));
    }

    /// <summary>
    /// With no trial, and in a replay that asks a live model, the tool runs as it is and nothing is recorded: a live
    /// model's new calls need the real tool.
    /// </summary>
    [Test]
    public async Task WithNoTrial_OrALiveModel_TheToolRuns()
    {
        object? alone = await Recorded(Search("3 lines")).InvokeAsync(Query("payment"));
        object? withLiveModel;
        using (Flow(WebReplayMode.Replay, model: false))
            withLiveModel = await Recorded(Search("4 lines")).InvokeAsync(Query("payment"));

        Assert.Multiple(() =>
        {
            Assert.That(Json(alone), Is.EqualTo(Json("3 lines")));
            Assert.That(Json(withLiveModel), Is.EqualTo(Json("4 lines")));
            Assert.That(_runs, Is.EqualTo(2));
            Assert.That(_folder.Trials, Is.Empty, "nothing recorded");
        });
    }

    /// <summary>With no store the tool comes back as it is; a tool that is only declared is never wrapped.</summary>
    [Test]
    public void WithNoStore_OrNoFunction_TheToolIsItself()
    {
        AIFunction search = Search("3 lines");
        AITool declared = AIFunctionFactory.CreateDeclaration("Rider", "A tool Rider runs.", default);

        Assert.Multiple(() =>
        {
            Assert.That(RecordedTool.Around(search, null), Is.SameAs(search));
            Assert.That(RecordedTool.Around(declared, _folder.Store), Is.SameAs(declared));
            Assert.That(RecordedTool.Around(search, _folder.Store).Name, Is.EqualTo("SearchDocs"));
        });
    }
}
