using System.Globalization;

namespace LouisAgent.Core.Web.Robots;

/// <summary>
/// What came back when we asked a site for its <c>robots.txt</c>: the HTTP status (none when nothing answered) and the
/// text (only for a good answer). A fetch that fails on the network is <c>RobotsFile(null, null)</c>, not an exception,
/// so the policy sees one shape for every outcome.
/// </summary>
public sealed record RobotsFile(int? Status, string? Text)
{
    /// <summary>
    /// Why the file counts as unreachable, in a word the model can pass on: the status, or the failure kind.
    /// </summary>
    public string Reason => Status is { } status
        ? status.ToString(CultureInfo.InvariantCulture)
        : nameof(HttpRequestError.ConnectionError);
}
