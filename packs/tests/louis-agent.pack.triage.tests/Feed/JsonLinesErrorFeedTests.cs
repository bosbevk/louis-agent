using LouisAgent.Packs.Triage.Feed;

namespace LouisAgent.Packs.Triage.Tests.Feed;

/// <summary>
/// The feed over a service's JSON-lines error log (moved from the orchestrator in F106.3.1): each error is handed out
/// until it is marked handled, the mark survives a restart, a line that isn't JSON is skipped, and an id that isn't a
/// plain token is ignored, since ids end up in branch names.
/// </summary>
public class JsonLinesErrorFeedTests
{
    private string _dir = null!;

    [SetUp]
    public void MakeAFolder()
    {
        _dir = Path.Combine(Path.GetTempPath(), $"triage-feed-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dir);
    }

    [TearDown]
    public void RemoveTheFolder() => Directory.Delete(_dir, recursive: true);

    /// <summary>One error as the sample service writes it.</summary>
    private static string Error(string id, string method = "order-total") =>
        $$"""{"id":"{{id}}","timestamp":"2026-10-06T10:00:00+00:00","service":"order-service","method":"{{method}}","arguments":"1003","exception_type":"System.Collections.Generic.KeyNotFoundException","message":"The given key 'SUMMER25' was not present","stack_trace":"   at OrderService.OrderApi.GetOrderTotal(Int32 id)"}""";

    /// <summary>An error is handed out until it is marked handled, and the mark is kept on disk.</summary>
    [Test]
    public void AnError_IsHandedOut_UntilItIsMarkedHandled()
    {
        string log = Path.Combine(_dir, "errors.jsonl");
        File.WriteAllLines(log, [Error("aaa111"), "not json", Error("bbb222")]);
        IErrorFeed feed = new JsonLinesErrorFeed(log, Path.Combine(_dir, "state"));

        Assert.That(feed.ReadNew().Select(e => e.Id), Is.EqualTo(new[] { "aaa111", "bbb222" }));
        Assert.That(feed.Find("aaa111")!.Method, Is.EqualTo("order-total"));

        feed.MarkHandled("aaa111");
        Assert.That(new JsonLinesErrorFeed(log, Path.Combine(_dir, "state")).ReadNew().Select(e => e.Id),
            Is.EqualTo(new[] { "bbb222" }), "handled ids survive a restart");
    }

    /// <summary>An id with anything but letters, digits, dashes and underscores is left alone.</summary>
    [Test]
    public void AnId_ThatIsntAPlainToken_IsIgnored()
    {
        string log = Path.Combine(_dir, "errors.jsonl");
        File.WriteAllLines(log, [Error("x; rm -rf /"), Error("ok_1")]);

        Assert.That(new JsonLinesErrorFeed(log, _dir).ReadNew().Select(e => e.Id), Is.EqualTo(new[] { "ok_1" }));
    }

    /// <summary>A log that isn't there yet means no errors, not a failure.</summary>
    [Test]
    public void AMissingLog_MeansNoErrors() =>
        Assert.That(new JsonLinesErrorFeed(Path.Combine(_dir, "none.jsonl"), _dir).ReadNew(), Is.Empty);
}
