using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tools.Web.Search;
using LouisAgent.Core.Sessions.Replay.Store;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Core.Tests.Tools.Web.Replay;

/// <summary>A temporary replay folder per test, a fixed clock, and fake browsers: one that answers a fixed visit and counts calls, one that throws.</summary>
internal sealed class ReplayFolder : IDisposable
{
    public static readonly DateTimeOffset At = new(2026, 10, 25, 6, 0, 0, TimeSpan.Zero);

    public string Path { get; } =
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "louis-replay-" + Guid.NewGuid().ToString("N"));

    public ReplayStore Store => new(Path, new FakeTimeProvider(At));

    public string[] Answers => Files("answers");

    public string[] Trials => Files("trials");

    /// <summary>Every file and folder under the replay folder (none when the folder was never made).</summary>
    public string[] Entries => Directory.Exists(Path)
        ? Directory.GetFileSystemEntries(Path, "*", SearchOption.AllDirectories)
        : [];

    private string[] Files(string sub) =>
        Directory.Exists(System.IO.Path.Combine(Path, sub))
            ? Directory.GetFiles(System.IO.Path.Combine(Path, sub))
            : [];

    public void Dispose()
    {
        if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
    }
}

/// <summary>A search provider that counts calls and answers fixed results, or throws: the inner provider of a replay test.</summary>
internal sealed class CountingSearch(
    IReadOnlyList<WebSearchResult>? results = null,
    Exception? throws = null,
    string name = "FakeSearch") : IWebSearchProvider
{
    public int Calls { get; private set; }

    public string Name => name;

    public Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        if (throws is not null) throw throws;
        return Task.FromResult(results ?? []);
    }
}

internal sealed class FakeBrowser(BrowserVisit? visit = null, Exception? throws = null) : IWebBrowser
{
    public int Calls { get; private set; }

    public Task<BrowserVisit> VisitAsync(Uri url, Func<Uri, bool>? capture = null, BrowserSession? session = null,
        string? script = null,
        string? readySelector = null, CancellationToken cancellationToken = default)
    {
        Calls++;
        if (throws is not null) throw throws;
        return Task.FromResult(visit ?? new BrowserVisit(url.AbsoluteUri, "<html>fake</html>", []));
    }
}
