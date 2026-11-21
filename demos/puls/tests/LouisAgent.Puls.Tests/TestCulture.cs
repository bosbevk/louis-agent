using System.Globalization;

// T1: the snapshots must never depend on the machine. Puls sets no culture of its own (its numbers are written by
// UsageFormat with the invariant culture), so the tests run invariant.
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
