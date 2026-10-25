// F18 step 4d: every core test runs as if an operator had turned content capture on. Microsoft.Extensions.AI reads this
// variable once per process (TelemetryHelpers.EnableSensitiveDataDefault is a static readonly), so a test that sets it
// halfway through changes nothing; only a fixture that runs before the first client is built can. With it on, the
// pipeline's explicit EnableSensitiveData = false is what keeps a user's words out of the spans, and the tests prove that.
[SetUpFixture]
public class TelemetryCapture
{
    public const string Variable = "OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT";

    private string? _found;

    [OneTimeSetUp]
    public void TurnOn()
    {
        _found = Environment.GetEnvironmentVariable(Variable);
        Environment.SetEnvironmentVariable(Variable, "true");
    }

    [OneTimeTearDown]
    public void PutBack() => Environment.SetEnvironmentVariable(Variable, _found);
}
