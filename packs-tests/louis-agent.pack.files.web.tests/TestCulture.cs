using System.Globalization;

namespace LouisAgent.Packs.Files.Web.Tests;

/// <summary>
/// The snapshots must never depend on the machine. The code-review app sets no culture of its own, so the tests run
/// invariant.
/// </summary>
[SetUpFixture]
public class TestCulture
{
    /// <summary>Sets the invariant culture before any test runs.</summary>
    [OneTimeSetUp]
    public void Fix()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }
}
