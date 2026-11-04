using System.Globalization;

// T1b: the snapshots must never depend on the machine. The code-review app sets no culture of its own, so the tests
// run invariant.
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
