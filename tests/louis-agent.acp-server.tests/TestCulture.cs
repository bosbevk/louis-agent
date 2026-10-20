using System.Globalization;

namespace LouisAgent.AcpServer.Tests;

/// <summary>The transcript is the same on every machine: numbers and dates in the invariant culture.</summary>
[SetUpFixture]
public class TestCulture
{
    [OneTimeSetUp]
    public void Invariant()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }
}
