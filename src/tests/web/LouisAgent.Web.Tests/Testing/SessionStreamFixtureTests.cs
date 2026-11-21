using LouisAgent.Web.Testing;

namespace LouisAgent.Web.Tests.Testing;

/// <summary>
/// F166.3: the recordings travel with the test helpers, beside their own DLL, so a site in a repository of its own
/// replays the same turns without louis-agent's source tree (Louis's pick A of finding 8).
/// </summary>
public class SessionStreamFixtureTests
{
    /// <summary>Both recordings and what the readers must read from them sit beside the test's own output.</summary>
    [Test]
    public void TheRecordings_SitBesideTheTestsOutput()
    {
        string[] files =
            [SessionStreamFixture.StreamFile, SessionStreamFixture.AskStreamFile, SessionStreamFixture.ExpectedFile];

        Assert.Multiple(() =>
        {
            foreach (string file in files)
            {
                Assert.That(file, Does.StartWith(AppContext.BaseDirectory));
                Assert.That(File.Exists(file), Is.True, file);
            }
        });
    }
}
