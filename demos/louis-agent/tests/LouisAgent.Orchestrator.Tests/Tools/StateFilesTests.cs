using LouisAgent.Orchestrator.Tools;
using LouisAgent.Orchestrator.Tools.Records;
using LouisAgent.Packs.Triage.Feed;

namespace LouisAgent.Orchestrator.Tests.Tools;

/// <summary>The orchestrator's jsonl lines, written through records with the keys every file has always used.</summary>
public class StateFilesTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 4, 18, 0, 0, TimeSpan.Zero);

    private string _dir = null!;

    /// <summary>Each test writes to a folder of its own.</summary>
    [SetUp]
    public void SetUp() => _dir = Directory.CreateTempSubdirectory("state-files-").FullName;

    /// <summary>Clears the test's folder.</summary>
    [TearDown]
    public void TearDown() => Directory.Delete(_dir, recursive: true);

    /// <summary>An escalation keeps the error's method, type and message beside the team and the reason.</summary>
    [Test]
    public void AnEscalation_IsOneSnakeCaseLine()
    {
        var error = new ServiceError("abc123", At, "order-service", "refund", "1003", "System.InvalidOperationException",
            "card ref missing", null);

        string line = Write(StateFiles.Escalations, EscalationRecord.Of(error, "payments", "never auto-fixed", At));

        Assert.That(line,
            Is.EqualTo(
                """{"error_id":"abc123","team":"payments","reason":"never auto-fixed","method":"refund","exception_type":"System.InvalidOperationException","message":"card ref missing","at":"2026-11-04T18:00:00+00:00"}"""));
    }

    /// <summary>An acknowledgement is the error, the runbook's reason, and when.</summary>
    [Test]
    public void AnAcknowledgement_IsOneSnakeCaseLine() =>
        Assert.That(Write(StateFiles.Acknowledged, new AcknowledgementRecord("abc123", "a 404 is expected", At)),
            Is.EqualTo("""{"error_id":"abc123","reason":"a 404 is expected","at":"2026-11-04T18:00:00+00:00"}"""));

    /// <summary>Lines are added, never rewritten.</summary>
    [Test]
    public void Append_AddsALine()
    {
        var files = new StateFiles(Path.Combine(_dir, "state"));

        files.Append(StateFiles.Acknowledged, new AcknowledgementRecord("a", "x", At));
        files.Append(StateFiles.Acknowledged, new AcknowledgementRecord("b", "y", At));

        Assert.That(File.ReadAllLines(Path.Combine(_dir, "state", StateFiles.Acknowledged)), Has.Length.EqualTo(2));
    }

    /// <summary>Writes one record to a fresh file and reads back its line.</summary>
    private string Write<T>(string fileName, T record)
    {
        string state = Path.Combine(_dir, Guid.NewGuid().ToString("N"));
        new StateFiles(state).Append(fileName, record);
        return File.ReadAllText(Path.Combine(state, fileName)).TrimEnd();
    }
}
