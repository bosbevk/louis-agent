using LouisAgent.Core.Logging;
using LouisAgent.Core.Telemetry;
using LouisAgent.Testing.Otlp;

namespace LouisAgent.Core.Telemetry.Tests;

/// <summary>
/// The last word of a process (F52 step 5, ADR 0064): an exception nothing caught is logged once, Critical, as the
/// exception itself (type, message and stack), and the logs and spans are sent before the process ends; a failed
/// task nobody awaited is an Error and is marked seen, as the process goes on (Louis's pick I1).
/// </summary>
/// <remarks>
/// A test can't raise the runtime's unhandled-exception event without ending the test host, so the tests call the
/// handler's methods as the events would. Not parallel: stderr, the logging and the <c>OTEL_*</c> variables belong
/// to the whole process.
/// </remarks>
[NonParallelizable]
public class LastChanceTests
{
    /// <summary>Where a collector is named, the standard way.</summary>
    private const string EndpointVariable = "OTEL_EXPORTER_OTLP_ENDPOINT";

    /// <summary>How the sink sends: HTTP with protobuf, which a plain listener can take.</summary>
    private const string ProtocolVariable = "OTEL_EXPORTER_OTLP_PROTOCOL";

    /// <summary>The console's own stderr, put back after each test.</summary>
    private TextWriter _error = null!;

    /// <summary>What the logging wrote to stderr during the test.</summary>
    private StringWriter _stderr = null!;

    /// <summary>Stderr captured, and logging configured to the console alone.</summary>
    [SetUp]
    public void SetUp()
    {
        _error = Console.Error;
        Console.SetError(_stderr = new StringWriter());
        AgentLogging.Configure(null);
    }

    /// <summary>The variables cleared, logging configured without them, and stderr put back.</summary>
    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable(EndpointVariable, null);
        Environment.SetEnvironmentVariable(ProtocolVariable, null);
        AgentLogging.Configure(null);
        Console.SetError(_error);
    }

    /// <summary>A crash is one Critical line, with the exception under it: its type, message and stack.</summary>
    [Test]
    public void ACrash_IsOneFatalLine_WithTheException()
    {
        LastChance.Handle(Thrown("boom"));

        string written = _stderr.ToString();
        Assert.That(Lines(written, "[FATAL]"), Has.Length.EqualTo(1));
        Assert.That(written, Does.Contain("InvalidOperationException: boom"));
        Assert.That(written, Does.Contain(" at "), "the stack, so the exception went to the logger, not its message");
    }

    /// <summary>A failed task nobody awaited is marked seen and logged as an Error, not a crash.</summary>
    [Test]
    public void ALostTask_IsMarkedSeen_AndIsAnError()
    {
        var lost = new UnobservedTaskExceptionEventArgs(new AggregateException(Thrown("lost")));

        LastChance.OnUnobserved(null, lost);

        string written = _stderr.ToString();
        Assert.That(lost.Observed, Is.True);
        Assert.That(Lines(written, "[ERROR]"), Has.Length.EqualTo(1));
        Assert.That(written, Does.Contain("lost").And.Not.Contain("[FATAL]"));
    }

    /// <summary>A failure someone said to expect is seen and left out of the errors; any other still counts.</summary>
    [Test]
    public void AnExpectedFailure_IsSeen_ButIsNoError()
    {
        LastChance.Expect(failure => failure.Message == "expected, in this test");
        var expected = new UnobservedTaskExceptionEventArgs(new AggregateException(Thrown("expected, in this test")));
        var mixed = new UnobservedTaskExceptionEventArgs(
            new AggregateException(Thrown("expected, in this test"), Thrown("something else")));

        LastChance.OnUnobserved(null, expected);
        string afterExpected = _stderr.ToString();
        LastChance.OnUnobserved(null, mixed);

        Assert.That(expected.Observed, Is.True);
        Assert.That(Lines(afterExpected, "[ERROR]"), Is.Empty);
        Assert.That(Lines(_stderr.ToString(), "[ERROR]"), Has.Length.EqualTo(1),
            "one that isn't expected still counts");
    }

    /// <summary>
    /// With a collector, the crash's line reaches it with no flush from anyone else: the process is about to end.
    /// </summary>
    [Test]
    public async Task ACrash_ReachesTheCollector_OnItsOwn()
    {
        using var receiver = new OtlpReceiver();
        Environment.SetEnvironmentVariable(EndpointVariable, receiver.Endpoint);
        Environment.SetEnvironmentVariable(ProtocolVariable, "http/protobuf");
        AgentLogging.Configure(null, also: OtlpTelemetry.Instance.AddSinks);

        LastChance.Handle(Thrown("boom"));

        Assert.That(await receiver.EventuallyHoldsAsync("boom"), Is.True);
    }

    /// <summary>An exception as a crash has one: thrown, so it carries a stack.</summary>
    private static InvalidOperationException Thrown(string message)
    {
        try
        {
            throw new InvalidOperationException(message);
        }
        catch (InvalidOperationException ex)
        {
            return ex;
        }
    }

    /// <summary>The lines of what was written that start with the tag.</summary>
    private static string[] Lines(string written, string tag) =>
        [.. written.Split('\n').Where(line => line.StartsWith(tag, StringComparison.Ordinal))];
}
