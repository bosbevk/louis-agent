using System.Net;
using System.Net.Http.Json;
using LouisAgent.Api.Evals.Conversations;
using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Runs.Json;
using LouisAgent.Evals.Contracts.Transcripts;
using LouisAgent.Evals.Runs.Store;
using Microsoft.Extensions.AI;
using static LouisAgent.Api.Evals.Tests.EvalsTestHost;

namespace LouisAgent.Api.Evals.Tests.Conversations;

/// <summary>
/// F126.4: a person flips a criterion's mark at the mark's own address, and puts it back with a DELETE there. The run
/// is changed in its file under the store's lock, so two flips at once both stay, and a run that is going is refused.
/// </summary>
public class FlipRoutesTests
{
    private const string KeptId = "20261123-043012-greets";
    private static readonly DateTimeOffset Started = new(2026, 11, 23, 4, 30, 12, TimeSpan.Zero);

    /// <summary>A flip with a note answers 204, and the run read after has the mark flipped, note and time.</summary>
    [Test]
    public async Task AFlip_WithANote_IsKeptOnTheMark()
    {
        await using var host = await HostWithAKeptRun();

        using var flip = await host.Client.PutAsJsonAsync(FlipAt(1, 1, 2), new FlipRequest("It does say so."),
            RunJson.Options);
        var mark = (await KeptRun(host)).Conversations[0].Repeats[0].Turns[0].Criteria[1];

        Assert.That(flip.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(mark.Flip?.Note, Is.EqualTo("It does say so."));
        Assert.That(mark.Flip!.At, Is.GreaterThan(Started));
        Assert.That(mark.CountsAsMet(), Is.True);
    }

    /// <summary>A flip needs no note: a PUT with no body flips the mark with none.</summary>
    [Test]
    public async Task AFlip_WithNoBody_IsKeptWithNoNote()
    {
        await using var host = await HostWithAKeptRun();

        using var flip = await host.Client.PutAsync(FlipAt(1, 1, 2), null);
        var mark = (await KeptRun(host)).Conversations[0].Repeats[0].Turns[0].Criteria[1];

        Assert.That(flip.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((mark.Flip is not null, mark.Flip?.Note), Is.EqualTo((true, (string?)null)));
    }

    /// <summary>A DELETE at the same address answers 204 and puts the judge's answer back.</summary>
    [Test]
    public async Task UndoingAFlip_PutsTheJudgesAnswerBack()
    {
        await using var host = await HostWithAKeptRun();
        using var flip = await host.Client.PutAsJsonAsync(FlipAt(1, 1, 2), new FlipRequest(null), RunJson.Options);

        using var undo = await host.Client.DeleteAsync(FlipAt(1, 1, 2));
        var run = await KeptRun(host);

        Assert.That(undo.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(run.CountFlips(), Is.Zero);
    }

    /// <summary>A run, a conversation, a repeat, a turn or a criterion the run doesn't have is a 404.</summary>
    [TestCase("20261123-043012-nobody", "hello", 1, 1, 1)]
    [TestCase(KeptId, "nobody", 1, 1, 1)]
    [TestCase(KeptId, "hello", 2, 1, 1)]
    [TestCase(KeptId, "hello", 1, 2, 1)]
    [TestCase(KeptId, "hello", 1, 1, 3)]
    public async Task AFlipOfAPlaceTheRunDoesNotHave_IsNotFound(string id, string conversation, int repeat, int turn,
        int criterion)
    {
        await using var host = await HostWithAKeptRun();
        string address = $"/v1/admin/evals/runs/{id}/{conversation}/{repeat}/{turn}/criteria/{criterion}/flip";

        using var flip = await host.Client.PutAsJsonAsync(address, new FlipRequest(null), RunJson.Options);
        using var undo = await host.Client.DeleteAsync(address);

        Assert.That((flip.StatusCode, undo.StatusCode), Is.EqualTo((HttpStatusCode.NotFound, HttpStatusCode.NotFound)));
    }

    /// <summary>A run that is going is being written, so its marks are refused with a 409 that names it.</summary>
    [Test]
    public async Task AFlipOfTheRunGoing_IsAConflict()
    {
        using var held = new ManualResetEventSlim();
        await using var host = await StartAsync(Waiting(held));
        using var started = await host.StartRunAsync();
        string id = started.Headers.Location!.ToString().Split('/')[^1];

        using var flip = await host.Client.PutAsJsonAsync(
            $"/v1/admin/evals/runs/{id}/hello/1/1/criteria/1/flip", new FlipRequest(null), RunJson.Options);
        held.Set();
        await host.EndedAsync(started.Headers.Location!);

        Assert.That(flip.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(await flip.Content.ReadAsStringAsync(), Does.Contain(id));
    }

    /// <summary>A note is one line of at most 200 characters; two lines, or more, is a 400.</summary>
    [TestCase("One line,\nthen another.")]
    [TestCase(null)]
    public async Task ANoteOfTwoLinesOrTooLong_IsABadRequest(string? note)
    {
        await using var host = await HostWithAKeptRun();

        using var flip = await host.Client.PutAsJsonAsync(FlipAt(1, 1, 2),
            new FlipRequest(note ?? new string('a', 201)), RunJson.Options);

        Assert.That(flip.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await KeptRun(host)).CountFlips(), Is.Zero);
    }

    /// <summary>Both routes answer 401 without the admin's key.</summary>
    [Test]
    public async Task BothRoutes_AnswerUnauthorized_WithoutTheAdminKey()
    {
        await using var host = await HostWithAKeptRun();
        host.Client.DefaultRequestHeaders.Remove("x-api-key");

        using var flip = await host.Client.PutAsJsonAsync(FlipAt(1, 1, 2), new FlipRequest(null), RunJson.Options);
        using var undo = await host.Client.DeleteAsync(FlipAt(1, 1, 2));

        Assert.That((flip.StatusCode, undo.StatusCode),
            Is.EqualTo((HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized)));
    }

    /// <summary>Two flips sent at once, on two marks of the same run, both stay: no save loses the other.</summary>
    [Test]
    public async Task TwoFlipsAtOnce_OnTwoMarksOfTheSameRun_BothStay()
    {
        await using var host = await HostWithAKeptRun();

        var both = await Task.WhenAll(
            host.Client.PutAsJsonAsync(FlipAt(1, 1, 1), new FlipRequest(null), RunJson.Options),
            host.Client.PutAsJsonAsync(FlipAt(1, 1, 2), new FlipRequest(null), RunJson.Options));
        foreach (var answer in both) answer.Dispose();

        Assert.That((await KeptRun(host)).CountFlips(), Is.EqualTo(2));
    }

    /// <summary>The address of a criterion's flip in the kept run's one conversation.</summary>
    private static string FlipAt(int repeat, int turn, int criterion) =>
        $"/v1/admin/evals/runs/{KeptId}/hello/{repeat}/{turn}/criteria/{criterion}/flip";

    /// <summary>The API, with one finished run kept: one turn, one fact and two criteria, one missed.</summary>
    private static async Task<EvalsTestHost> HostWithAKeptRun()
    {
        var host = await StartAsync(Answering("Hello."));
        var transcript = new TurnTranscript("Hello?", "Hello.", [], TimeSpan.FromSeconds(2));
        var turn = new TurnRun(transcript, [new Mark("no-error", true, "No error.")],
            [new Mark("Greets back", true, "It does."), new Mark("Says its name", false, "It doesn't.")], null, null);
        var kept = new EvalRun(KeptId, Eval, Agent, RunKind.Live, 1, Started,
            [new ConversationRun("hello", 1, [new RepeatRun(1, [turn])])])
        {
            Status = RunStatus.Finished,
            EndedAt = Started.AddMinutes(1)
        };
        new RunStore(host.RunsFolder).Save(kept);
        return host;
    }

    /// <summary>The kept run, read at its address.</summary>
    private static async Task<EvalRun> KeptRun(EvalsTestHost host) =>
        (await host.Client.GetFromJsonAsync<EvalRun>($"/v1/admin/evals/runs/{KeptId}", RunJson.Options))!;

    /// <summary>A model that waits until the test lets it answer, so the run stays going.</summary>
    private static FakeChatClient Waiting(ManualResetEventSlim held) => new(Enumerable.Repeat<Func<IList<ChatMessage>,
        ChatResponse>>(_ =>
    {
        held.Wait(TimeSpan.FromSeconds(30));
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello."));
    }, 5).ToArray());
}
