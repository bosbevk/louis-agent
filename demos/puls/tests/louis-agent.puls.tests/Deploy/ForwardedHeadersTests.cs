namespace LouisAgent.Puls.Tests.Deploy;

/// <summary>
/// Both Azure apps read the scheme the visitor used from the proxy in front of them (FX68, Louis's screenshot:
/// Google's "Error 400: redirect_uri_mismatch"). Container Apps ends https at its ingress and passes the request on as
/// http, so without the forwarded headers the app told Google to send the person back to <c>http://…/signin-google</c>,
/// which isn't the https address the Google client allows. ASP.NET Core's own switch turns the forwarded headers on.
/// </summary>
/// <remarks>Plain string checks on the file's lines, no regex (ADR 0057).</remarks>
public class ForwardedHeadersTests
{
    /// <summary>The app's yaml sets the switch, and sets it on.</summary>
    [TestCase("puls-app.yaml")]
    [TestCase("louis-agent-app.yaml")]
    public void TheApp_ReadsTheForwardedScheme(string yaml)
    {
        var lines = File.ReadAllLines(Path.Combine(TestPaths.RepoRoot, "demos/puls/deploy/azure", yaml))
            .Select(line => line.Trim()).ToList();
        int name = lines.IndexOf("- name: ASPNETCORE_FORWARDEDHEADERS_ENABLED");

        Assert.That(name, Is.GreaterThanOrEqualTo(0), "the switch is missing");
        Assert.That(lines[name + 1], Is.EqualTo("value: 'true'"));
    }
}
