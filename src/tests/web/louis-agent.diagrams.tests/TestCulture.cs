using System.Globalization;

namespace LouisAgent.Diagrams.Tests;

/// <summary>
/// A drawing's numbers must never depend on the machine. The layout writes them invariant, and a test that builds
/// the number it expects does it in the current culture, so the tests run invariant, as the web's do.
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
