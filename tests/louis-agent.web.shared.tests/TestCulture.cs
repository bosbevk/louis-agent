using System.Globalization;

// T1: the snapshots must never depend on the machine. The web apps set no culture of their own (UsageFormat writes its
// numbers with the invariant culture; the few N0 formats follow the current one), so the tests run invariant.
[SetUpFixture]
public class TestCulture
{
    [OneTimeSetUp]
    public void Fix()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }
}
