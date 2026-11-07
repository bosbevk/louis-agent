using LouisAgent.Web.Testing;

namespace LouisAgent.Web.Tests;

/// <summary>
/// What the snapshot helper takes out of a component's markup before the golden sees it: a component with its own
/// styles (CL58) carries Blazor's scope attribute on every element it draws, a hash of its file's path that would
/// change the golden when the file moves.
/// </summary>
public class SnapshotsTests
{
    [Test]
    public void TheScopeAttribute_IsNormalised() =>
        Assert.That(Snapshots.Normalise("<div b-k3x9z2m1qa class=\"account-shell\"><main b-k3x9z2m1qa>x</main></div>"),
            Is.EqualTo("<div b-SCOPE class=\"account-shell\"><main b-SCOPE>x</main></div>\n"));

    [Test]
    public void AWordThatLooksLikeAScope_InText_Stays() =>
        Assert.That(Snapshots.Normalise("<p>the b-side, and b-k3x9z2m1qa too</p>"),
            Is.EqualTo("<p>the b-side, and b-k3x9z2m1qa too</p>\n"));
}
