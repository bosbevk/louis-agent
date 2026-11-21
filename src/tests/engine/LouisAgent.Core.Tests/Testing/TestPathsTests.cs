namespace LouisAgent.Core.Tests.Testing;

/// <summary>
/// The demos a test lists are the folders under <c>demos/</c>, never what a run leaves there for a moment. FX131: the
/// starter's spawn test makes <c>demos/.spawn-…</c> while it runs, and the site's demos diagram test, counting the
/// demos at the same time, counted it too.
/// </summary>
public class TestPathsTests
{
    /// <summary>A dot-folder is a run's leftover, so it's never a demo, and every other folder is one.</summary>
    [Test]
    public void ADotFolder_IsNotADemo()
    {
        string folder = Directory.CreateTempSubdirectory("demos-").FullName;
        try
        {
            Directory.CreateDirectory(Path.Combine(folder, "acme"));
            Directory.CreateDirectory(Path.Combine(folder, "zeta"));
            Directory.CreateDirectory(Path.Combine(folder, ".spawn-1"));

            var demos = TestPaths.Demos(folder).Select(Path.GetFileName);

            Assert.That(demos, Is.EqualTo(new[] { "acme", "zeta" }));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
