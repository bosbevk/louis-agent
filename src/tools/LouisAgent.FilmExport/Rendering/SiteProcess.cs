using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using LouisAgent.Composition.Messages;
using LouisAgent.FilmExport.Messages;
using LouisAgent.FilmExport.Messages.Holes;

namespace LouisAgent.FilmExport.Rendering;

/// <summary>
/// A site, running on a spare port for as long as an export needs it, from the code as it is now: louis-agent.dev's
/// host for the films and that site's card, and the framework's host for its page cards (F169.6).
/// </summary>
public sealed class SiteProcess : IDisposable
{
    private static readonly TimeSpan Limit = TimeSpan.FromMinutes(3);

    private readonly Process _process;

    private SiteProcess(Process process, Uri address)
    {
        _process = process;
        Address = address;
    }

    /// <summary>louis-agent.dev's host, from the repo's root.</summary>
    public const string BusinessSite = "src/site/LouisAgent.SiteApi";

    /// <summary>What dotnet is asked, to run a host from the repo with none of its launch profiles.</summary>
    public static IReadOnlyList<string> Arguments(string repoRoot, string host) =>
        ["run", "--no-launch-profile", "--project", Path.Combine(repoRoot, host)];

    /// <summary>Where it answers.</summary>
    public Uri Address { get; }

    /// <summary>
    /// Starts this host, a project's folder from the repo's root, and waits until it answers, or throws if it stops or
    /// takes too long.
    /// </summary>
    public static async Task<SiteProcess> StartAsync(string repoRoot, string host)
    {
        var address = new Uri($"http://127.0.0.1:{SparePort()}/");
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            // Development, because that's where a host run from its build folder finds the app's stylesheets.
            Environment =
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["ASPNETCORE_URLS"] = address.AbsoluteUri
            },
        };
        foreach (string argument in Arguments(repoRoot, host))
            start.ArgumentList.Add(argument);

        var site = new SiteProcess(Process.Start(start)!, address);
        await site.WaitUntilItAnswersAsync();
        return site;
    }

    /// <summary>Stops it, and anything it started.</summary>
    public void Dispose()
    {
        _process.Kill(entireProcessTree: true);
        _process.Dispose();
    }

    /// <summary>Asks its health page until it answers.</summary>
    private async Task WaitUntilItAnswersAsync()
    {
        using var http = new HttpClient();
        var stop = Stopwatch.StartNew();
        while (stop.Elapsed < Limit && !_process.HasExited)
        {
            try
            {
                using var reply = await http.GetAsync(new Uri(Address, "health"));
                if (reply.IsSuccessStatusCode) return;
            }
            catch (HttpRequestException)
            {
                // Not listening yet.
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        var notReady = new SiteNotReadyHoles(Address);
        throw new InvalidOperationException(MessageText.Format(FilmExportUserText.SiteNotReady, notReady));
    }

    /// <summary>A port nothing is using right now.</summary>
    private static int SparePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
