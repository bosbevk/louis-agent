using LouisAgent.Choices;

namespace LouisAgent.Web.Shell;

/// <summary>Why the browser's script couldn't name a place. The script names the problem, and the site's words say it.</summary>
public enum LocationProblem
{
    [WireName("no-browser")] NoBrowser,
    [WireName("denied")] Denied,
    [WireName("not-found")] NotFound,
    [WireName("not-named")] NotNamed,
    [WireName("unreachable")] Unreachable,
}
